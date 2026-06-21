# ⚔️ THE FALLEN KNIGHT ⚔️

**The Fallen Knight** là một tựa game nhập vai hành động (Action RPG) 2D màn hình ngang mang phong cách giả tưởng đen tối (Dark Fantasy) được phát triển trên nền tảng **Unity**. Trò chơi kết hợp phong cách mỹ thuật Retro Pixel truyền thống cùng với cơ chế chiến đấu chặt chém hấp dẫn, hệ thống âm thanh sống động và cốt truyện sâu sắc phân nhánh nhiều kết cục.

---

## 📜 Cốt Truyện & Bối Cảnh (Lore)
> *Hàng nghìn năm trước, Hiệp sĩ Thánh **Aurelius** là người anh hùng đã đánh bại Quỷ Vương và cứu lấy lục địa Elyndor.*
>
> *Sau chiến thắng vinh quang, các vị thần đã từ chối đưa ông lên Thiên Giới vì linh hồn ông được sinh ra từ chiến tranh và máu đổ. Không thể chết, cũng không thể được cứu rỗi, Aurelius lang thang vô định suốt nhiều thế kỷ.*
>
> *Qua thời gian, khát vọng bảo vệ nhân loại ban đầu dần biến thành cơn ám ảnh phải chiến đấu không ngừng. Ông tin rằng chỉ có chiến tranh mới giữ cho thế giới này tồn tại. Người anh hùng năm xưa dần trở thành mối hiểm họa lớn nhất của nhân loại.*
>
> *Người chơi vào vai **The Nameless** – một chiến binh trẻ mang trong mình **Soul of Dawn** (Linh hồn Bình Minh) được các vị thần tạo ra nhằm chấm dứt vòng lặp chiến tranh vô tận này và giải thoát cho Hiệp sĩ Thánh Aurelius.*

---

## 🎮 Hướng Dẫn Điều Khiển (Controls)

| Phím bấm | Hành động trong game |
| :--- | :--- |
| **A / D** (hoặc Phím Mũi Tên) | Di chuyển sang Trái / Phải |
| **W** (hoặc Phím Lên / Space) | Nhảy lên (Jump) |
| **J** (hoặc Chuột Trái) | Tấn công thường (Melee Attack) |
| **K** (hoặc Chuột Phải) | Sử dụng kỹ năng đặc biệt (Special Skill) |
| **1 / 2 / 3** | Uống Bình Máu / Thể Lực / Năng Lượng |
| **ESC / P (khi chơi)** | Tạm dừng game (Pause Menu) |
| **P** | **Mở bảng Hack/Cheat Tool** (Bật/tắt chế độ bất tử, tăng sát thương, hồi máu nhanh để thử nghiệm game) |

---

## ✨ Các Tính Năng Nổi Bật

### 1. Hệ thống Phân Nhánh Kết Cục (Multiple Endings)
Sau khi đánh bại Trùm Cuối (Boss Thánh Hiệp Sĩ), người chơi sẽ đối mặt với sự lựa chọn định đoạt vận mệnh thế giới:
*   🟢 **Redemption Ending (Cứu Rỗi - Phím tắt/Nút bấm lựa chọn trái)**:
    *   *Kịch bản*: Cảnh Bóng ma đen quỳ xuống, lưỡi hái vỡ vụn, ánh sáng thánh thanh tẩy bóng tối thành các hạt vàng bay lên trời.
    *   *Video clip*: `ending_redemption.mp4`.
*   🔴 **Legacy Ending (Kế Thừa Bóng Tối - Phím tắt/Nút bấm lựa chọn phải)**:
    *   *Kịch bản*: Cảnh nhân vật chính nhặt lưỡi hái lên, hắc khí quấn quanh người và mắt hóa đỏ, vòng lặp chiến tranh lặp lại.
    *   *Video clip*: `ending_legacy.mp4`.

### 2. Giao Diện Dark Fantasy & Font Chữ Pixel
*   Tích hợp font chữ game pixel cổ điển (`PressStart2P`) thống nhất cho toàn bộ giao diện từ **Menu chính, Màn hình Pause, Màn hình Game Over** cho tới **Bảng lựa chọn Ending**.
*   Thiết kế HUD hiển thị lượng Máu (HP), Năng lượng (MP) và **Số lượng bình thuốc (Potion Count)** trực quan, sinh động.

### 3. Trình Quản Lý Âm Thanh (Audio Manager)
Hệ thống âm thanh phân tách kênh BGM (Nhạc nền) và SFX (Hiệu ứng) chân thực:
*   Phát nhạc nền u ám dồn dập khi chiến đấu với Boss.
*   SFX sinh động khi nhân vật Tấn công (Hit), Bị thương (Hurt), Nhảy (Jump), Ăn tiền vàng (Coin), Thắng trận (Victory) và Thua cuộc (Game Over).

---

## 🛠️ Hướng Dẫn Cấu Hình Cho Lập Trình Viên (Developer Guide)

Để thiết lập giao diện và đồng bộ toàn bộ tài nguyên game một cách tự động, nhóm phát triển đã xây dựng công cụ Editor chuyên dụng. Bạn chỉ cần thực hiện các bước sau:

1.  Mở dự án **The Fallen Knight** bằng Unity Editor.
2.  Trên thanh Menu trên cùng, chọn:
    👉 **Tools** $\rightarrow$ **Setup Menus (Dark Fantasy)**
3.  Công cụ sẽ tự động thực hiện:
    *   Mở cảnh chơi game (`SampleScene`), thiết lập các Canvas Gameplay, Pause Menu, Game Over, và bảng chọn kết cục Boss Defeated Panel.
    *   Tự động khởi tạo và lưu cảnh màn hình chính (`MainMenuScene`) với đầy đủ hiệu ứng Slideshow giới thiệu cốt truyện & chạy Video Intro chuyển dạng của Boss.
    *   Tự động cấu hình danh sách cảnh trong **Build Settings** theo đúng thứ tự (`MainMenuScene` ở vị trí số `0` và `SampleScene` ở vị trí số `1`).
    *   Tự động import và cấu hình định dạng Sprite cho các hình ảnh nền.

---

## 📁 Cấu Trúc Thư Mục Quan Trọng
```text
Assets/
├── _Project/
│   ├── Audio/               # Tài nguyên âm thanh hiệu ứng (SFX & BGM)
│   ├── Fonts/               # Font chữ Pixel PressStart2P-Regular
│   ├── Scenes/              # Chứa MainMenuScene & SampleScene
│   ├── Scripts/             
│   │   ├── Editor/          # Chứa SetupUIEditor (Công cụ Setup tự động)
│   │   ├── Player/          # Điều khiển nhân vật, chỉ số và bình thuốc
│   │   ├── Enemy/           # Trí tuệ nhân tạo quái vật & chỉ số Boss
│   │   ├── Audio/           # Quản lý âm thanh AudioManager
│   │   └── UI/              # Quản lý Menu, Pause, Game Over, Cutscene
│   └── Sprites/             # Sprite ảnh nền giao diện Dark Fantasy
└── video/                   # Chứa các đoạn phim cắt cảnh mp4 (Intro, Endings)
```

---
*Chúc bạn có những trải nghiệm tuyệt vời cùng **The Fallen Knight**!*
