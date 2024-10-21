import cv2
import mediapipe as mp
import numpy as np
import socket
import json
import os
import math
from mediapipe.framework.formats import landmark_pb2

# MediaPipe 솔루션 초기화
mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles
FaceLandmarker = mp.tasks.vision.FaceLandmarker
HandLandmarker = mp.tasks.vision.HandLandmarker
PoseLandmarker = mp.tasks.vision.PoseLandmarker
FaceLandmarkerOptions = mp.tasks.vision.FaceLandmarkerOptions
HandLandmarkerOptions = mp.tasks.vision.HandLandmarkerOptions
PoseLandmarkerOptions = mp.tasks.vision.PoseLandmarkerOptions
VisionRunningMode = mp.tasks.vision.RunningMode

# 모델 다운로드 함수
def download_model(model_path, url):
    if not os.path.exists(model_path):
        print(f"Downloading model to {model_path}...")
        import urllib.request
        urllib.request.urlretrieve(url, model_path)
        print("Model downloaded successfully.")

# 모델 경로 설정
current_dir = os.path.dirname(os.path.abspath(__file__))
face_model_path = os.path.join(current_dir, 'face_landmarker.task')
hand_model_path = os.path.join(current_dir, 'hand_landmarker.task')
pose_model_path = os.path.join(current_dir, 'pose_landmarker.task')

# 모델 다운로드
download_model(face_model_path, 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task')
download_model(hand_model_path, 'https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/1/hand_landmarker.task')
download_model(pose_model_path, 'https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task')

# 랜드마커 초기화
face_landmarker = FaceLandmarker.create_from_options(FaceLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=face_model_path),
    output_face_blendshapes=True,
    output_facial_transformation_matrixes=True,
    num_faces=1
))

hand_landmarker = HandLandmarker.create_from_options(HandLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=hand_model_path),
    num_hands=2
))

pose_landmarker = PoseLandmarker.create_from_options(PoseLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=pose_model_path),
    output_segmentation_masks=True
))

cap = cv2.VideoCapture(0)

# 소켓 생성
sock_pose = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_hand = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_face = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_pose_address = ('127.0.0.1', 5052)  # UnityChanPoseController용 포트
unity_hand_address = ('127.0.0.1', 5053)  # MediapipeHandMapper용 포트
unity_face_address = ('127.0.0.1', 5054)  # Face 데이터용 포트

# 프레임 카운터 초기화
frame_counter = 0

# 데이터 저장 디렉토리 생성
output_dir = os.path.join(current_dir, 'output_data')
if not os.path.exists(output_dir):
    os.makedirs(output_dir)

# 표정 판정에 필요한 함수 및 데이터 정의
# 각 표정에 대한 특징 벡터 정의 (값 조정)
expressions = {
    "neutral": {
        # 모든 블렌드셰이프 값이 거의 0에 가까움
        "features": {}
    },
    "angry": {
        "features": {
            "browDownLeft": 0.8,
            "browDownRight": 0.8,
            "browInnerUp": 0.2,
            "eyeSquintLeft": 0.5,
            "eyeSquintRight": 0.5,
            "mouthFrownLeft": 0.5,
            "mouthFrownRight": 0.5,
            "jawForward": 0.3
        }
    },
    "fun": {
        "features": {
            "mouthSmileLeft": 0.7,
            "mouthSmileRight": 0.7,
            "cheekSquintLeft": 0.5,
            "cheekSquintRight": 0.5,
            "eyeSquintLeft": 0.3,
            "eyeSquintRight": 0.3
        }
    },
    "joy": {
        "features": {
            "mouthSmileLeft": 1.0,
            "mouthSmileRight": 1.0,
            "cheekSquintLeft": 0.5,
            "cheekSquintRight": 0.5,
            "eyeWideLeft": 0.2,
            "eyeWideRight": 0.2
        }
    },
    "sorrow": {
        "features": {
            "browInnerUp": 0.7,
            "mouthFrownLeft": 0.6,
            "mouthFrownRight": 0.6,
            "jawOpen": 0.2
        }
    },
    "surprised": {
        "features": {
            "browInnerUp": 1.0,
            "browOuterUpLeft": 1.0,
            "browOuterUpRight": 1.0,
            "eyeWideLeft": 1.0,
            "eyeWideRight": 1.0,
            "jawOpen": 1.0
        }
    }
}

# 임계값 설정 (값을 높이면 판정이 더 유연해집니다)
EXPRESSION_THRESHOLD = 0.6

# 가중치 스케일링 팩터 (변동폭 조절)
BLENDSHAPE_WEIGHT_SCALE_FACTOR = 1.5  # 일반 블렌드 쉐이프용
MOUTH_SHAPE_WEIGHT_SCALE_FACTOR = 1.6  # 입 모양 블렌드 쉐이프용

# 가중치 최소 임계값
WEIGHT_THRESHOLD = 0.35
WEIGHT_THRESHOLD_MOUTH = 0.1

# 감쇠 계수 (0과 1 사이, 값이 작을수록 변화가 느려짐)
SMOOTHING_FACTOR = 0.9  # 블렌드쉐이프 가중치에 대한 감쇠 계수
SMOOTHING_FACTOR_EXPRESSION = 0.6  # 표정 점수에 대한 감쇠 계수

# 이전 프레임의 데이터 저장용 변수 초기화
prev_blendshape_data = None
prev_expression_scores = None

# 표정 점수 계산 함수
def compute_expression_scores(blendshape_data):
    expression_scores = {}

    for expression_name, expression_data in expressions.items():
        distance = 0.0
        for key in expression_data["features"]:
            target_value = expression_data["features"].get(key, 0.0)
            current_value = blendshape_data.get(key, 0.0)
            distance += (target_value - current_value) ** 2
        distance = math.sqrt(distance)
        # 특징 벡터의 크기로 정규화
        norm = math.sqrt(sum([v ** 2 for v in expression_data["features"].values()]))
        if norm > 0:
            similarity = 1 - (distance / norm)
        else:
            similarity = 0.0
        # 0~1 사이로 클램핑
        similarity = max(0.0, min(1.0, similarity))
        # 스케일링 팩터 적용
        similarity *= BLENDSHAPE_WEIGHT_SCALE_FACTOR
        # 최대값을 1로 클램핑
        similarity = min(similarity, 1.0)
        # 임계값 이하인 경우 0으로 설정
        if similarity < WEIGHT_THRESHOLD:
            similarity = 0.0
        expression_scores[expression_name] = similarity

    return expression_scores

# 입 모양 계산 함수 수정
def compute_mouth_shapes(blendshape_data):
    mouth_shapes = {}
    # 간단한 로직; 필요에 따라 조정
    mouth_open = blendshape_data.get("jawOpen", 0.0)
    mouth_funnel = blendshape_data.get("mouthFunnel", 0.0)
    mouth_pucker = blendshape_data.get("mouthPucker", 0.0)
    mouth_wide = blendshape_data.get("mouthStretchLeft", 0.0) + blendshape_data.get("mouthStretchRight", 0.0)

    # 가중치 스케일링 팩터 적용 (입 모양용)
    mouth_open *= MOUTH_SHAPE_WEIGHT_SCALE_FACTOR
    mouth_funnel *= MOUTH_SHAPE_WEIGHT_SCALE_FACTOR
    mouth_pucker *= MOUTH_SHAPE_WEIGHT_SCALE_FACTOR
    mouth_wide *= MOUTH_SHAPE_WEIGHT_SCALE_FACTOR

    # 최대값을 1로 클램핑
    mouth_open = min(mouth_open, 1.0)
    mouth_funnel = min(mouth_funnel, 1.0)
    mouth_pucker = min(mouth_pucker, 1.0)
    mouth_wide = min(mouth_wide, 1.0)

    # 임계값 이하인 경우 0으로 설정
    if mouth_open < WEIGHT_THRESHOLD_MOUTH:
        mouth_open = 0.0
    if mouth_funnel < WEIGHT_THRESHOLD_MOUTH:
        mouth_funnel = 0.0
    if mouth_pucker < WEIGHT_THRESHOLD_MOUTH:
        mouth_pucker = 0.0
    if mouth_wide < WEIGHT_THRESHOLD_MOUTH:
        mouth_wide = 0.0

    # 각 모음에 대한 가중치 계산 (값은 조정 필요)
    mouth_shapes["mouthShapeA"] = mouth_open * (1 - mouth_pucker)
    mouth_shapes["mouthShapeI"] = mouth_wide * mouth_open
    mouth_shapes["mouthShapeU"] = mouth_pucker * mouth_open
    mouth_shapes["mouthShapeE"] = mouth_open * (1 - mouth_pucker) * mouth_wide
    mouth_shapes["mouthShapeO"] = mouth_funnel * mouth_open

    # 각 모음 가중치에 임계값 적용
    for key in mouth_shapes:
        # 최대값을 1로 클램핑
        mouth_shapes[key] = min(mouth_shapes[key], 1.0)
        # 임계값 이하인 경우 0으로 설정
        if mouth_shapes[key] < WEIGHT_THRESHOLD_MOUTH:
            mouth_shapes[key] = 0.0

    return mouth_shapes

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    frame_counter += 1  # 프레임 카운터 증가

    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image_rgb)

    face_result = face_landmarker.detect(mp_image)
    hand_result = hand_landmarker.detect(mp_image)
    pose_result = pose_landmarker.detect(mp_image)

    height, width, _ = image.shape
    black_image = np.zeros((height, width, 3), dtype=np.uint8)

    data = {}
    expression = "neutral"  # 기본값 설정

    # 얼굴 랜드마크 처리
    if face_result.face_landmarks:
        face_landmarks = face_result.face_landmarks[0]

        # NormalizedLandmarkList 생성 및 변환
        face_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        for lmk in face_landmarks:
            landmark = landmark_pb2.NormalizedLandmark()
            landmark.x = lmk.x
            landmark.y = lmk.y
            landmark.z = lmk.z
            face_landmarks_proto.landmark.append(landmark)

        mp_drawing.draw_landmarks(
            image=black_image,
            landmark_list=face_landmarks_proto,
            connections=mp.solutions.face_mesh.FACEMESH_TESSELATION,
            landmark_drawing_spec=None,
            connection_drawing_spec=mp_drawing_styles.get_default_face_mesh_tesselation_style()
        )

    if face_result.face_blendshapes:
        blendshapes = face_result.face_blendshapes[0]
        current_blendshape_data = {bs.category_name: bs.score for bs in blendshapes}

        # 블렌드 쉐이프 가중치 스케일링 팩터 적용 및 임계값 이하인 경우 0으로 설정
        for key in current_blendshape_data:
            # 입 모양 관련 블렌드 쉐이프는 제외
            if key in ["jawOpen", "mouthFunnel", "mouthPucker", "mouthStretchLeft", "mouthStretchRight"]:
                continue
            # 스케일링 팩터 적용
            current_blendshape_data[key] *= BLENDSHAPE_WEIGHT_SCALE_FACTOR
            # 최대값을 1로 클램핑
            current_blendshape_data[key] = min(current_blendshape_data[key], 1.0)
            # 임계값 이하인 경우 0으로 설정
            if current_blendshape_data[key] < WEIGHT_THRESHOLD:
                current_blendshape_data[key] = 0.0

        # 이전 프레임과 현재 프레임의 블렌드쉐이프 가중치를 보간하여 부드럽게 변화
        if prev_blendshape_data is None:
            smoothed_blendshape_data = current_blendshape_data.copy()
        else:
            smoothed_blendshape_data = {}
            for key in current_blendshape_data:
                prev_value = prev_blendshape_data.get(key, 0.0)
                current_value = current_blendshape_data[key]
                smoothed_value = prev_value * (1 - SMOOTHING_FACTOR) + current_value * SMOOTHING_FACTOR
                smoothed_blendshape_data[key] = smoothed_value

        # 이전 프레임의 블렌드쉐이프 데이터 업데이트
        prev_blendshape_data = smoothed_blendshape_data.copy()

        data['blendshapes'] = smoothed_blendshape_data

        # 표정 점수 계산 및 'BlendShapeWeights' 데이터 추가
        expression_scores = compute_expression_scores(smoothed_blendshape_data)

        # 이전 프레임과 현재 프레임의 표정 점수를 보간하여 부드럽게 변화
        if prev_expression_scores is None:
            smoothed_expression_scores = expression_scores.copy()
        else:
            smoothed_expression_scores = {}
            for key in expression_scores:
                prev_value = prev_expression_scores.get(key, 0.0)
                current_value = expression_scores[key]
                smoothed_value = prev_value * (1 - SMOOTHING_FACTOR_EXPRESSION) + current_value * SMOOTHING_FACTOR_EXPRESSION
                smoothed_expression_scores[key] = smoothed_value

        # 이전 프레임의 표정 점수 업데이트
        prev_expression_scores = smoothed_expression_scores.copy()

        data['BlendShapeWeights'] = smoothed_expression_scores

        # 가장 높은 점수의 표정을 선택
        expression = max(smoothed_expression_scores, key=smoothed_expression_scores.get)
        data['expression'] = expression

        # BlendShapeWeights 중 하나라도 0이 아닌지 확인
        is_expression_active = any(value > 0.2 for value in smoothed_expression_scores.values())

        # 표정이 적용되는 경우 눈 깜빡임을 0으로 설정
        if is_expression_active:
            smoothed_blendshape_data['eyeBlinkLeft'] = 0.0
            smoothed_blendshape_data['eyeBlinkRight'] = 0.0

        # 입 모양 계산 및 추가
        mouth_shapes = compute_mouth_shapes(current_blendshape_data)

        # 입 모양도 부드럽게 보간
        if 'mouthShapes' not in prev_blendshape_data:
            smoothed_mouth_shapes = mouth_shapes.copy()
        else:
            smoothed_mouth_shapes = {}
            for key in mouth_shapes:
                prev_value = prev_blendshape_data.get(key, 0.0)
                current_value = mouth_shapes[key]
                smoothed_value = prev_value * (1 - SMOOTHING_FACTOR) + current_value * SMOOTHING_FACTOR
                smoothed_mouth_shapes[key] = smoothed_value

        smoothed_blendshape_data.update(smoothed_mouth_shapes)

        # 이전 프레임의 입 모양 데이터 업데이트
        prev_blendshape_data.update(smoothed_mouth_shapes)

    # 손 랜드마크 처리
    if hand_result.hand_landmarks:
        for idx, hand_landmarks in enumerate(hand_result.hand_landmarks):
            hand_landmarks_list = [[lmk.x, lmk.y, lmk.z] for lmk in hand_landmarks]
            data[f'hand_{idx}'] = hand_landmarks_list

            # NormalizedLandmarkList 생성 및 변환
            hand_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
            for lmk in hand_landmarks:
                landmark = landmark_pb2.NormalizedLandmark()
                landmark.x = lmk.x
                landmark.y = lmk.y
                landmark.z = lmk.z
                hand_landmarks_proto.landmark.append(landmark)

            mp_drawing.draw_landmarks(
                black_image,
                hand_landmarks_proto,
                mp.solutions.hands.HAND_CONNECTIONS,
                mp_drawing_styles.get_default_hand_landmarks_style(),
                mp_drawing_styles.get_default_hand_connections_style()
            )

    # 포즈 랜드마크 처리
    if pose_result.pose_landmarks:  # 변경됨: 포즈 랜드마크가 존재할 때만 처리
        pose_landmarks = pose_result.pose_landmarks[0]
        pose_landmarks_list = [[lmk.x, lmk.y, lmk.z] for lmk in pose_landmarks]
        data['pose'] = pose_landmarks_list

        # 특정 인덱스의 랜드마크를 제외하고 그리기
        pose_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        excluded_indices = set(range(0, 11)) | set(range(17, 23))  # 0~10, 17~22
        index_mapping = {}
        new_idx = 0
        for idx, lmk in enumerate(pose_landmarks):
            if idx not in excluded_indices:
                landmark = landmark_pb2.NormalizedLandmark()
                landmark.x = lmk.x
                landmark.y = lmk.y
                landmark.z = lmk.z
                pose_landmarks_proto.landmark.append(landmark)
                index_mapping[idx] = new_idx
                new_idx += 1

        # 그리기 위한 연결 필터링 및 인덱스 재매핑
        filtered_connections = []
        for connection in mp.solutions.pose.POSE_CONNECTIONS:
            idx1, idx2 = connection
            if (idx1 not in excluded_indices) and (idx2 not in excluded_indices):
                new_idx1 = index_mapping[idx1]
                new_idx2 = index_mapping[idx2]
                filtered_connections.append((new_idx1, new_idx2))

        mp_drawing.draw_landmarks(
            black_image,
            pose_landmarks_proto,
            filtered_connections,
            landmark_drawing_spec=mp_drawing_styles.get_default_pose_landmarks_style()
        )

    # 데이터 전송 부분 수정
    if data:
        # 포즈 데이터 전송
        if 'pose' in data:
            pose_data = {'pose': data['pose']}
            json_pose_data = json.dumps(pose_data)
            sock_pose.sendto(json_pose_data.encode(), unity_pose_address)
        else:
            # 포즈 데이터가 없으면 모델이 T-포즈를 유지하도록 아무 동작도 하지 않음
            pass  # 변경됨: 포즈 데이터가 없을 때는 전송하지 않음

        # 손 데이터 전송
        if 'hand_0' in data or 'hand_1' in data:
            hand_data = {k: v for k, v in data.items() if k.startswith('hand_')}
            json_hand_data = json.dumps(hand_data)
            sock_hand.sendto(json_hand_data.encode(), unity_hand_address)

        # 얼굴 데이터 전송
        if 'blendshapes' in data or 'expression' in data or 'BlendShapeWeights' in data:
            face_data = {}
            if 'blendshapes' in data:
                face_data['blendshapes'] = data['blendshapes']
            if 'expression' in data:
                face_data['expression'] = data['expression']
            if 'BlendShapeWeights' in data:
                face_data['BlendShapeWeights'] = data['BlendShapeWeights']
            json_face_data = json.dumps(face_data)
            sock_face.sendto(json_face_data.encode(), unity_face_address)
            # 데이터 저장
            # face_file_path = os.path.join(output_dir, f'face_data_{frame_counter}.json')
            # with open(face_file_path, 'w') as f:
            #     f.write(json_face_data)

    # 결과 이미지 표시
    cv2.imshow('MediaPipe Multi-model Landmarker', cv2.flip(black_image, 1))
    if cv2.waitKey(5) & 0xFF == 27:
        break

cap.release()
face_landmarker.close()
hand_landmarker.close()
pose_landmarker.close()
