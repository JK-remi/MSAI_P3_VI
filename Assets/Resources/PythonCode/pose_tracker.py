import cv2
import mediapipe as mp
import numpy as np
import socket
import json
import os
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

# 기존 코드에서 소켓을 세 개 생성하여 각각 다른 포트로 데이터 전송
sock_pose = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_hand = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_face = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_pose_address = ('127.0.0.1', 5052)  # UnityChanPoseController용 포트
unity_hand_address = ('127.0.0.1', 5053)  # MediapipeHandMapper용 포트
unity_face_address = ('127.0.0.1', 5054)  # Face 데이터용 포트

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=image_rgb)

    face_result = face_landmarker.detect(mp_image)
    hand_result = hand_landmarker.detect(mp_image)
    pose_result = pose_landmarker.detect(mp_image)

    height, width, _ = image.shape
    black_image = np.zeros((height, width, 3), dtype=np.uint8)

    data = {}

    # 얼굴 랜드마크 처리
    if face_result.face_landmarks:
        face_landmarks = face_result.face_landmarks[0]
        face_landmarks_list = [[lmk.x, lmk.y, lmk.z] for lmk in face_landmarks]
        data['face'] = face_landmarks_list

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
        blendshape_data = {bs.category_name: bs.score for bs in blendshapes}
        data['blendshapes'] = blendshape_data

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
    if pose_result.pose_landmarks:
        pose_landmarks = pose_result.pose_landmarks[0]
        pose_landmarks_list = [[lmk.x, lmk.y, lmk.z] for lmk in pose_landmarks]
        data['pose'] = pose_landmarks_list

        # 특정 인덱스의 랜드마크를 제외하고 그리기
        pose_landmarks_proto = landmark_pb2.NormalizedLandmarkList()
        excluded_indices = set(range(0, 11)) | set(range(17, 23))  # 0~10, 15~22
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
        # 손 데이터 전송
        if 'hand_0' in data or 'hand_1' in data:
            hand_data = {k: v for k, v in data.items() if k.startswith('hand_')}
            json_hand_data = json.dumps(hand_data)
            sock_hand.sendto(json_hand_data.encode(), unity_hand_address)
        # 얼굴 데이터 전송
        if 'face' in data or 'blendshapes' in data:
            face_data = {}
            if 'face' in data:
                face_data['face'] = data['face']
            if 'blendshapes' in data:
                face_data['blendshapes'] = data['blendshapes']
            json_face_data = json.dumps(face_data)
            sock_face.sendto(json_face_data.encode(), unity_face_address)

    # 결과 이미지 표시
    cv2.imshow('MediaPipe Multi-model Landmarker', cv2.flip(black_image, 1))
    if cv2.waitKey(5) & 0xFF == 27:
        break

cap.release()
face_landmarker.close()
hand_landmarker.close()
pose_landmarker.close()
