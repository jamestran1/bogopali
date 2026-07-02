# Học tập & Đúc kết Kiến thức (Learning & Archiving)

Tài liệu này lưu trữ các bài học kinh nghiệm về cú pháp Rust, tư duy thiết kế hệ thống và các quyết định kiến trúc được rút ra sau mỗi Task trong dự án `bogopali`.

---

## Task 1: Project Scaffolding & Configuration Loader

### 1. Kiến thức cú pháp Rust đã học

#### Trait và tính Đa hình (Polymorphism)
- **Trait làm Interface:** Rất giống với `interface` trong C#. Ta định nghĩa bản hợp đồng hành vi:
  ```rust
  pub trait ConfigStorage {
      fn resolve_path(&self) -> PathBuf;
      fn read_raw(&self) -> Option<String>;
      fn write_raw(&self, content: &str) -> Result<(), std::io::Error>;
  }
  ```
- **Triển khai Trait:** Tách biệt hoàn toàn dữ liệu (`struct`) và hành vi (`impl`). Khai báo liên kết bằng từ khóa `for`:
  ```rust
  impl ConfigStorage for FileConfigStorage { ... }
  ```
- **Triển khai Trait cho Kiểu Tham chiếu (`&T`):** Giúp truyền tham chiếu của Storage vào các hàm Generics một cách linh hoạt mà không bị hạn chế bởi Ownership:
  ```rust
  impl<'a, T: ConfigStorage + ?Sized> ConfigStorage for &'a T { ... }
  ```

#### Cú pháp Khớp mẫu (Pattern Matching)
- Sử dụng cấu trúc `match` thay thế cho chuỗi `if-else` dài để bóc tách kiểu dữ liệu an toàn `Result` và `Option` từ thư viện ngoài (như `toml::from_str`):
  ```rust
  match toml::from_str::<TomlConfig>(&raw) {
      Ok(parsed) => { ... }
      Err(e) => {
          eprintln!("[bogopali] Warning: Failed to parse config.toml: {}. Falling back to defaults.", e);
      }
  }
  ```

---

### 2. Tư duy Thiết kế Hệ thống (Software Design)

#### Nguyên lý Đóng gói và Module Sâu (Deep Module)
- **Thiết kế dung hòa:** Người gọi bên ngoài (như hàm `main`) chỉ cần giao diện cực kỳ đơn giản và trực quan:
  - `Config::load_from_path(path)`
  - `config.save_to_path(path)`
- **Ẩn giấu sự phức tạp (Encapsulation):** Toàn bộ cơ chế trừu tượng hóa I/O (`ConfigStorage`), gộp cấu hình mặc định (Merge-and-Override), tự động ghi file mẫu nếu thiếu, và chuyển đổi chuỗi rỗng `""` để tắt phím tắt đều được che giấu hoàn toàn bên trong lớp nghiệp vụ.

#### Khả năng Kiểm thử độc lập (Testability)
- Nhờ tách biệt hành vi đọc ghi qua Trait, chúng ta có thể giả lập bộ lưu trữ RAM (`MemoryConfigStorage`) trong Unit Test, giúp chạy test nhanh hơn, độc lập và không để lại file rác trên đĩa cứng của hệ thống.
- Với các kiểm thử tích hợp thực sự cần kiểm tra I/O trên đĩa, ta sử dụng thư mục tạm của hệ điều hành (`std::env::temp_dir()`) để tránh ảnh hưởng đến file cấu hình thật của ứng dụng.
