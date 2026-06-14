import cv2
import numpy as np
from PIL import Image

MAP1 = r"E:\AlienOgKoApp\Code\AlienOgKo.Unity\Assets\Map\map1.png"
MAP2 = r"E:\AlienOgKoApp\Code\AlienOgKo.Unity\Assets\Map\map2.png"
OUT  = r"E:\AlienOgKoApp\Code\AlienOgKo.Unity\Assets\Map\map.png"

img1 = cv2.imread(MAP1)
img2 = cv2.imread(MAP2)

gray1 = cv2.cvtColor(img1, cv2.COLOR_BGR2GRAY)
gray2 = cv2.cvtColor(img2, cv2.COLOR_BGR2GRAY)

orb = cv2.ORB_create(5000)
kp1, des1 = orb.detectAndCompute(gray1, None)
kp2, des2 = orb.detectAndCompute(gray2, None)

matcher = cv2.BFMatcher(cv2.NORM_HAMMING, crossCheck=True)
matches = matcher.match(des1, des2)
matches = sorted(matches, key=lambda m: m.distance)
good = matches[:200]

pts1 = np.float32([kp1[m.queryIdx].pt for m in good])
pts2 = np.float32([kp2[m.trainIdx].pt for m in good])

# Find translation only (map2 relative to map1)
H, mask = cv2.findHomography(pts2, pts1, cv2.RANSAC, 5.0)
inliers = mask.ravel().sum()
print(f"Matched {len(good)} features, {inliers} inliers")
print(f"Homography:\n{H}")

h1, w1 = img1.shape[:2]
h2, w2 = img2.shape[:2]

# Transform corners of img2 into img1's coordinate space
corners2 = np.float32([[0,0],[w2,0],[w2,h2],[0,h2]]).reshape(-1,1,2)
corners2_t = cv2.perspectiveTransform(corners2, H)
print(f"map2 corners in map1 space: {corners2_t.reshape(-1,2)}")

# Compute canvas bounds
all_corners = np.vstack([
    [[0,0],[w1,0],[w1,h1],[0,h1]],
    corners2_t.reshape(-1,2)
])
x_min = int(np.floor(all_corners[:,0].min()))
y_min = int(np.floor(all_corners[:,1].min()))
x_max = int(np.ceil(all_corners[:,0].max()))
y_max = int(np.ceil(all_corners[:,1].max()))

offset_x = -x_min
offset_y = -y_min
canvas_w = x_max - x_min
canvas_h = y_max - y_min
print(f"Canvas size before power-of-2: {canvas_w} x {canvas_h}, offset ({offset_x}, {offset_y})")

# Warp img2 onto canvas
T = np.array([[1,0,offset_x],[0,1,offset_y],[0,0,1]], dtype=np.float64)
H_shifted = T @ H

canvas = np.zeros((canvas_h, canvas_w, 3), dtype=np.uint8)
warped2 = cv2.warpPerspective(img2, H_shifted, (canvas_w, canvas_h))

# Place img1 on canvas
canvas[offset_y:offset_y+h1, offset_x:offset_x+w1] = img1

# Blend: use img1 where it exists, otherwise warped2
mask1 = np.zeros((canvas_h, canvas_w), dtype=np.uint8)
mask1[offset_y:offset_y+h1, offset_x:offset_x+w1] = 255
result = np.where(mask1[:,:,None] > 0, canvas, warped2)

# Crop to content (remove empty rows/cols)
gray_result = cv2.cvtColor(result, cv2.COLOR_BGR2GRAY)
rows = np.any(gray_result > 0, axis=1)
cols = np.any(gray_result > 0, axis=0)
r0, r1 = np.where(rows)[0][[0,-1]]
c0, c1 = np.where(cols)[0][[0,-1]]
result = result[r0:r1+1, c0:c1+1]
print(f"Cropped content size: {result.shape[1]} x {result.shape[0]}")

# Resize to power-of-2 (fit inside 2048x2048, maintain aspect ratio with padding)
TARGET = 2048
h, w = result.shape[:2]
scale = TARGET / max(h, w)
new_w = int(round(w * scale))
new_h = int(round(h * scale))
resized = cv2.resize(result, (new_w, new_h), interpolation=cv2.INTER_LANCZOS4)

final = np.zeros((TARGET, TARGET, 3), dtype=np.uint8)
pad_x = (TARGET - new_w) // 2
pad_y = (TARGET - new_h) // 2
final[pad_y:pad_y+new_h, pad_x:pad_x+new_w] = resized
print(f"Final texture: {TARGET}x{TARGET}, content at ({pad_x},{pad_y}) size {new_w}x{new_h}")

cv2.imwrite(OUT, final)
print(f"Saved: {OUT}")
