use std::collections::HashMap;
use std::path::PathBuf;

// 1. Định nghĩa Struct chứa dữ liệu cấu hình
#[derive(Debug, Clone, PartialEq)]
pub struct Config {
    pub hotkey: String,
    pub rules: HashMap<String, String>,
}

// 2. Định nghĩa Trait quy định hành vi lưu trữ (Interface)
pub trait ConfigStorage {
    fn resolve_path(&self) -> PathBuf;
    fn read_raw(&self) -> Option<String>;
    fn write_raw(&self, content: &str) -> Result<(), std::io::Error>;
}

impl<'a, T: ConfigStorage + ?Sized> ConfigStorage for &'a T {
    fn resolve_path(&self) -> PathBuf {
        (**self).resolve_path()
    }
    fn read_raw(&self) -> Option<String> {
        (**self).read_raw()
    }
    fn write_raw(&self, content: &str) -> Result<(), std::io::Error> {
        (**self).write_raw(content)
    }
}

// 3. Định nghĩa Struct dựng cấu hình (Builder)
impl Config {
    pub fn default_hotkey() -> String {
        "Alt + `".to_string()
    }

    pub fn default_rules() -> HashMap<String, String> {
        let mut rules = HashMap::new();
        rules.insert("aa".to_string(), "ā".to_string());
        rules.insert("ii".to_string(), "ī".to_string());
        rules.insert("uu".to_string(), "ū".to_string());
        rules.insert("dd".to_string(), "ḍ".to_string());
        rules.insert("tt".to_string(), "ṭ".to_string());
        rules.insert("nn".to_string(), "ṇ".to_string());
        rules.insert("ll".to_string(), "ḷ".to_string());
        rules.insert("mm".to_string(), "ṃ".to_string());
        rules.insert("nj".to_string(), "ñ".to_string());
        rules.insert("nq".to_string(), "ṅ".to_string());
        rules
    }
}

impl Default for Config {
    fn default() -> Self {
        Self {
            hotkey: Self::default_hotkey(),
            rules: Self::default_rules(),
        }
    }
}

#[derive(serde::Serialize, serde::Deserialize)]
struct TomlConfig {
    hotkey: Option<String>,
    rules: Option<HashMap<String, String>>,
}

pub struct ConfigBuilder<S: ConfigStorage> {
    storage: S,
}

impl<S: ConfigStorage> ConfigBuilder<S> {
    pub fn new(storage: S) -> Self {
        Self { storage }
    }

    pub fn build(self) -> Config {
        let mut config = Config::default();
        if let Some(raw) = self.storage.read_raw() {
            match toml::from_str::<TomlConfig>(&raw) {
                Ok(parsed) => {
                    if let Some(hotkey) = parsed.hotkey {
                        if !hotkey.trim().is_empty() {
                            config.hotkey = hotkey;
                        }
                    }
                    if let Some(rules) = parsed.rules {
                        for (k, v) in rules {
                            if v.is_empty() {
                                config.rules.remove(&k);
                            } else {
                                config.rules.insert(k, v);
                            }
                        }
                    }
                }
                Err(e) => {
                    eprintln!("[bogopali] Warning: Failed to parse config.toml: {}. Falling back to defaults.", e);
                }
            }
        } else {
            // Config file is missing: write default config to storage
            let default_config = Config::default();
            if let Err(e) = default_config.save_with(&self.storage) {
                eprintln!("[bogopali] Warning: Failed to write default config: {}", e);
            }
        }
        config
    }
}

pub struct FileConfigStorage {
    filename: String,
}

impl FileConfigStorage {
    pub fn new(filename: impl Into<String>) -> Self {
        Self { filename: filename.into() }
    }
}

impl ConfigStorage for FileConfigStorage {
    fn resolve_path(&self) -> PathBuf {
        let path = PathBuf::from(&self.filename);
        if path.is_absolute() {
            path
        } else {
            let mut base_dir = std::env::current_exe()
                .ok()
                .and_then(|p| p.parent().map(|parent| parent.to_path_buf()))
                .unwrap_or_else(|| std::env::current_dir().unwrap_or_else(|_| PathBuf::from(".")));
            base_dir.push(&self.filename);
            base_dir
        }
    }

    fn read_raw(&self) -> Option<String> {
        let path = self.resolve_path();
        std::fs::read_to_string(path).ok()
    }

    fn write_raw(&self, content: &str) -> Result<(), std::io::Error> {
        let path = self.resolve_path();
        if let Some(parent) = path.parent() {
            std::fs::create_dir_all(parent)?;
        }
        std::fs::write(path, content)
    }
}

impl Config {
    pub fn load_from_path(path: &std::path::Path) -> Self {
        let storage = FileConfigStorage::new(path.to_str().unwrap_or_default());
        ConfigBuilder::new(storage).build()
    }

    pub fn save_to_path(&self, path: &std::path::Path) -> Result<(), std::io::Error> {
        let storage = FileConfigStorage::new(path.to_str().unwrap_or_default());
        self.save_with(&storage)
    }

    pub fn save_with<S: ConfigStorage>(&self, storage: &S) -> Result<(), std::io::Error> {
        let mut serialized_rules = self.rules.clone();
        
        // Detect and explicitly mark disabled default rules
        for default_trigger in Self::default_rules().keys() {
            if !serialized_rules.contains_key(default_trigger) {
                serialized_rules.insert(default_trigger.clone(), String::new());
            }
        }

        let toml_data = TomlConfig {
            hotkey: Some(self.hotkey.clone()),
            rules: Some(serialized_rules),
        };

        let toml_str = toml::to_string_pretty(&toml_data)
            .map_err(|e| std::io::Error::new(std::io::ErrorKind::InvalidData, e))?;

        storage.write_raw(&toml_str)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    // Cấu trúc Storage giả lập trong bộ nhớ RAM để phục vụ viết test
    struct MemoryConfigStorage {
        data: Option<String>,
    }

    impl ConfigStorage for MemoryConfigStorage {
        fn resolve_path(&self) -> PathBuf {
            PathBuf::from("memory.toml")
        }

        fn read_raw(&self) -> Option<String> {
            self.data.clone()
        }

        fn write_raw(&self, _content: &str) -> Result<(), std::io::Error> {
            Ok(())
        }
    }

    #[test]
    fn test_default_config_via_empty_storage() {
        // Khởi tạo storage trống (RAM)
        let storage = MemoryConfigStorage { data: None };
        
        // Dựng config từ storage trống này
        let config = ConfigBuilder::new(storage).build();

        // Kiểm tra xem cấu hình có tự động trả về các giá trị mặc định hay không
        assert_eq!(config.hotkey, "Alt + `");
        assert_eq!(config.rules.len(), 10);
        assert_eq!(config.rules.get("aa").unwrap(), "ā");
    }

    #[test]
    fn test_custom_config_merging() {
        let toml_content = r#"
            hotkey = "Ctrl + Shift + P"
            [rules]
            aa = "custom_a"
            sh = "ṣ"
        "#;
        let storage = MemoryConfigStorage { data: Some(toml_content.to_string()) };
        let config = ConfigBuilder::new(storage).build();

        assert_eq!(config.hotkey, "Ctrl + Shift + P");
        assert_eq!(config.rules.get("aa").unwrap(), "custom_a");
        assert_eq!(config.rules.get("sh").unwrap(), "ṣ");
        assert_eq!(config.rules.get("ii").unwrap(), "ī");
        assert_eq!(config.rules.len(), 11);
    }

    #[test]
    fn test_disabling_rules() {
        let toml_content = r#"
            [rules]
            aa = ""
        "#;
        let storage = MemoryConfigStorage { data: Some(toml_content.to_string()) };
        let config = ConfigBuilder::new(storage).build();

        assert!(config.rules.get("aa").is_none());
        assert_eq!(config.rules.get("ii").unwrap(), "ī");
        assert_eq!(config.rules.len(), 9);
    }

    #[test]
    fn test_invalid_toml_fallback() {
        let invalid_content = r#"
            hotkey = 
            [rules
            aa =
        "#;
        let storage = MemoryConfigStorage { data: Some(invalid_content.to_string()) };
        let config = ConfigBuilder::new(storage).build();

        assert_eq!(config, Config::default());
    }

    #[test]
    fn test_file_config_storage_io() {
        let temp_dir = std::env::temp_dir();
        let path = temp_dir.join("bogopali_test_io_config.toml");
        let path_str = path.to_str().unwrap().to_string();
        
        if path.exists() {
            std::fs::remove_file(&path).unwrap();
        }

        let storage = FileConfigStorage::new(&path_str);
        // 1. Build from missing file -> should return default Config and auto-create file on disk
        let config = ConfigBuilder::new(&storage).build();
        assert_eq!(config, Config::default());
        assert!(path.exists());

        // 2. Modify config and save to disk
        let mut custom_config = config;
        custom_config.hotkey = "Ctrl + Alt + K".to_string();
        custom_config.rules.remove("aa"); // Disable default rule 'aa'
        custom_config.rules.insert("sh".to_string(), "ṣ".to_string()); // Add new rule

        custom_config.save_with(&storage).unwrap();

        // 3. Read it back via storage and build a new Config
        let new_storage = FileConfigStorage::new(&path_str);
        let loaded_config = ConfigBuilder::new(new_storage).build();

        assert_eq!(loaded_config.hotkey, "Ctrl + Alt + K");
        assert!(loaded_config.rules.get("aa").is_none());
        assert_eq!(loaded_config.rules.get("sh").unwrap(), "ṣ");
        assert_eq!(loaded_config.rules.get("ii").unwrap(), "ī");
        assert_eq!(loaded_config.rules.len(), 10); // 10 defaults - 1 disabled + 1 added = 10

        // Cleanup
        std::fs::remove_file(&path).unwrap();
    }

    #[test]
    fn test_public_path_apis() {
        let temp_dir = std::env::temp_dir();
        let path = temp_dir.join("bogopali_public_api_config.toml");
        
        if path.exists() {
            std::fs::remove_file(&path).unwrap();
        }

        // 1. load_from_path -> creates file and returns default Config
        let config = Config::load_from_path(&path);
        assert_eq!(config, Config::default());
        assert!(path.exists());

        // 2. save_to_path
        let mut custom_config = config;
        custom_config.hotkey = "Ctrl + F12".to_string();
        custom_config.save_to_path(&path).unwrap();

        // 3. load_from_path again
        let loaded = Config::load_from_path(&path);
        assert_eq!(loaded.hotkey, "Ctrl + F12");

        // Cleanup
        std::fs::remove_file(&path).unwrap();
    }
}
