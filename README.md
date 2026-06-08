# 🎌 PG-ContentManager

**Редактор контента для википедии аниме-тегов**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Platform](https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge&logo=windows)

---

##  О проекте

PG-ContentManager — это десктопное приложение для создания и редактирования JSON файлов, 
которые используются на сайте [playgame-studio.ru](https://playgame-studio.ru) в разделе 
аниме-википедии.

### Что умеет:
-  Создавать новые JSON файлы с нуля
- 📥 Загружать существующие JSON файлы
- ✏️ Редактировать теги и категории
- 👁️ Предварительный просмотр как на сайте
- 💾 Сохранять изменения
- 🗑️ Удалять теги и пользовательские категории

---

##  Установка

### Вариант 1: Скачать готовый .exe
1. Перейди в раздел [Releases](https://github.com/GREYCATNERO/PG-ContentManager/releases)
2. Скачай последнюю версию
3. Запусти `AnimeTagsEditor.exe`

**Требования:** 
- Windows 10/11
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### Вариант 2: Собрать из исходного кода
```bash
# Клонируем репозиторий
git clone https://github.com/GREYCATNERO/PG-ContentManager.git

# Переходим в папку проекта
cd PG-ContentManager

# Собираем проект
dotnet build -c Release

# Запускаем
dotnet run --project AnimeTagsEditor.csproj
