import cv2
import mediapipe as mp
import numpy as np
import socket
import json

mp_holistic = mp.solutions.holistic
mp_face_mesh = mp.solutions.face_mesh  # 추가된 부분
mp_drawing = mp.solutions.drawing_utils
holistic = mp_holistic.Holistic(
    static_image_mode=False,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5
)

cap = cv2.VideoCapture(0)

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_address = ('127.0.0.1', 5052)

# 필요한 얼굴 랜드마크 인덱스 추출
# 사용하는 연결들 정의
face_connections = (
    mp_face_mesh.FACEMESH_LIPS |
    mp_face_mesh.FACEMESH_LEFT_EYE |
    mp_face_mesh.FACEMESH_RIGHT_EYE |
    mp_face_mesh.FACEMESH_LEFT_EYEBROW |
    mp_face_mesh.FACEMESH_RIGHT_EYEBROW
)

# 연결들에서 인덱스 추출
face_indices = set()
for connection in face_connections:
    face_indices.update(connection)

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    # image = cv2.flip(image, 1)
    results = holistic.process(image_rgb)

    height, width, _ = image.shape
    black_image = np.zeros((height, width, 3), dtype=np.uint8)

    data = {}
    # 포즈 랜드마크 수집 및 그리기
    if results.pose_landmarks:
        pose_landmarks = [[lmk.x, lmk.y, lmk.z] for lmk in results.pose_landmarks.landmark]
        # pose_landmarks = [[lmk.x, lmk.y, lmk.z] for lmk in results.pose_world_landmarks.landmark]
        data['pose'] = pose_landmarks
        mp_drawing.draw_landmarks(
            black_image,
            results.pose_landmarks,
            mp_holistic.POSE_CONNECTIONS,
            mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2, circle_radius=2),
            mp_drawing.DrawingSpec(color=(0, 255, 0), thickness=2)
        )

    # 왼손 랜드마크 수집 및 그리기
    if results.left_hand_landmarks:
        left_hand_landmarks = [[lmk.x, lmk.y, lmk.z] for lmk in results.left_hand_landmarks.landmark]
        data['left_hand'] = left_hand_landmarks
        mp_drawing.draw_landmarks(
            black_image,
            results.left_hand_landmarks,
            mp_holistic.HAND_CONNECTIONS,
            mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2, circle_radius=2),
            mp_drawing.DrawingSpec(color=(255, 0, 0), thickness=2)
        )

    # 오른손 랜드마크 수집 및 그리기
    if results.right_hand_landmarks:
        right_hand_landmarks = [[lmk.x, lmk.y, lmk.z] for lmk in results.right_hand_landmarks.landmark]
        data['right_hand'] = right_hand_landmarks
        mp_drawing.draw_landmarks(
            black_image,
            results.right_hand_landmarks,
            mp_holistic.HAND_CONNECTIONS,
            mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2, circle_radius=2),
            mp_drawing.DrawingSpec(color=(0, 0, 255), thickness=2)
        )

    # 얼굴 랜드마크 수집 및 그리기 (필요한 부분만)
    if results.face_landmarks:
        # 필요한 얼굴 랜드마크만 수집
        face_landmarks = [results.face_landmarks.landmark[i] for i in face_indices]
        face_landmarks = [[lmk.x, lmk.y, lmk.z] for lmk in face_landmarks]
        data['face'] = face_landmarks
        # 얼굴 랜드마크 그리기
        mp_drawing.draw_landmarks(
            black_image,
            results.face_landmarks,
            face_connections,
            mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=1, circle_radius=1),
            mp_drawing.DrawingSpec(color=(0, 255, 255), thickness=1)
        )

    # 데이터 준비 및 전송
    if data:
        json_data = json.dumps(data)
        sock.sendto(json_data.encode(), unity_address)

    # 결과 이미지 표시
    cv2.imshow('MediaPipe Holistic', cv2.flip(black_image, 1))
    if cv2.waitKey(5) & 0xFF == 27:
        break

cap.release()
