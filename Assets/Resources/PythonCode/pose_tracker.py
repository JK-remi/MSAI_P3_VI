import cv2
import mediapipe as mp
import numpy as np
import socket
import json

mp_pose = mp.solutions.pose
mp_drawing = mp.solutions.drawing_utils  # 추가
pose = mp_pose.Pose(static_image_mode=False, min_detection_confidence=0.5, min_tracking_confidence=0.5)

cap = cv2.VideoCapture(0)

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_address = ('127.0.0.1', 5052)

while cap.isOpened():
    success, image = cap.read()
    if not success:
        continue

    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    results = pose.process(image)

    # 이미지의 크기 가져오기
    height, width, _ = image.shape

    # 검은 배경 이미지 생성
    black_image = np.zeros((height, width, 3), dtype=np.uint8)

    if results.pose_landmarks:
        landmarks = results.pose_landmarks.landmark
        data = {i: [landmark.x, landmark.y, landmark.z] for i, landmark in enumerate(landmarks)}
        json_data = json.dumps(data)
        sock.sendto(json_data.encode(), unity_address)

        # 랜드마크와 연결선을 검은 배경에 그림
        mp_drawing.draw_landmarks(
            black_image,
            results.pose_landmarks,
            mp_pose.POSE_CONNECTIONS,
            mp_drawing.DrawingSpec(color=(255, 255, 255), thickness=2, circle_radius=2),
            mp_drawing.DrawingSpec(color=(0, 0, 255), thickness=2)
        )

    # 결과 이미지 표시
    cv2.imshow('MediaPipe Pose', cv2.flip(black_image, 1))
    if cv2.waitKey(5) & 0xFF == 27:
        break

cap.release()
