import cv2
import numpy as np

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

# Compute per-match translation (map2 -> map1 space)
translations = pts1 - pts2

# RANSAC: find inlier consensus translation
best_tx, best_ty, best_inliers = 0, 0, 0
THRESHOLD = 5.0
for tx, ty in translations:
    dists = np.sqrt(((translations - [tx, ty]) ** 2).sum(axis=1))
    inliers = (dists < THRESHOLD).sum()
    if inliers > best_inliers:
        best_inliers = inliers
        best_tx, best_ty = tx, ty

# Refine using mean of inliers
dists = np.sqrt(((translations - [best_tx, best_ty]) ** 2).sum(axis=1))
inlier_mask = dists < THRESHOLD
tx = translations[inlier_mask, 0].mean()
ty = translations[inlier_mask, 1].mean()
print(f"Translation: dx={tx:.1f}, dy={ty:.1f} ({inlier_mask.sum()} inliers from {len(good)} matches)")

# Integer offsets for map2 top-left in map1's coordinate space
dx = int(round(tx))
dy = int(round(ty))

h1, w1 = img1.shape[:2]
h2, w2 = img2.shape[:2]

# Canvas bounds
x_min = min(0, dx)
y_min = min(0, dy)
x_max = max(w1, dx + w2)
y_max = max(h1, dy + h2)

ox = -x_min
oy = -y_min
canvas_w = x_max - x_min
canvas_h = y_max - y_min
print(f"Canvas: {canvas_w}x{canvas_h}, map1 at ({ox},{oy}), map2 at ({ox+dx},{oy+dy})")

canvas = np.zeros((canvas_h, canvas_w, 3), dtype=np.uint8)

# Place map2 first (bottom layer), then map1 on top
m2x, m2y = ox + dx, oy + dy
canvas[m2y:m2y+h2, m2x:m2x+w2] = img2
canvas[oy:oy+h1, ox:ox+w1] = img1

# Trim black borders: find the intersection column range where both maps contribute
# so we get a clean rectangle with no black edge gaps
left  = max(ox, m2x)
right = min(ox + w1, m2x + w2)
top   = oy
bottom = m2y + h2

result = canvas[top:bottom, left:right]
print(f"Trimmed content: {result.shape[1]}x{result.shape[0]}")

# Resize directly to 2048x2048 — no padding, whole texture is map
TARGET = 2048
h, w = result.shape[:2]
print(f"Content aspect ratio: {w}x{h} ({w/h:.3f})")
final = cv2.resize(result, (TARGET, TARGET), interpolation=cv2.INTER_LANCZOS4)
print(f"Final: {TARGET}x{TARGET}")

cv2.imwrite(OUT, final)
print(f"Saved: {OUT}")
