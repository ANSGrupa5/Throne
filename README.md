# Struktura Projektu

## Drzewo Katalogów
```text
Assets/
└── _Project/
    ├── _Art/
    │   ├── Animations/
    │   ├── Environments/
    │   ├── Materials/
    │   ├── Models/
    │   ├── Shaders/
    │   ├── Textures/
    │   └── VFX/
    ├── _Common/
    │   └── Enums/
    ├── _Core/
    │   ├── Editor/
    │   ├── Networking/
    │   ├── Plugins/
    │   └── Systems/
    ├── _Data/
    │   ├── ScriptableObjects/
    │   └── Settings/
    ├── _Debug/
    ├── _Features/
    │   ├── AI/
    │   ├── Customization/
    │   ├── PowerUps/
    │   ├── TrailSystem/
    │   └── Vehicle/
    ├── _Prefabs/
    │   ├── Environment/
    │   ├── Networking/
    │   ├── Vehicles/
    │   └── VFX/
    ├── _Scenes/
    ├── _Settings/
    └── _UI/
        ├── Fonts/
        └── Icons/
```
---

## Opis Zawartości

### 🎨 _Art
Zasoby wizualne i graficzne.
* **Animations**: Klipy, kontrolery animatorów.
* **Environments**: Modele i tekstury otoczenia.
* **Materials**: Materiały Unity.
* **Models**: Surowe pliki geometrii (.fbx, .obj).
* **Shaders**: Pliki Shader Graph / HLSL.
* **Textures**: Mapy tekstur.
* **VFX**: Systemy cząsteczkowe (VFX Graph / Shuriken).

### ⚙️ _Core
Kluczowe systemy i fundamenty gry.
* **Editor**: Skrypty rozszerzające edytor (nie trafiają do buildu).
* **Networking**: Zarządzanie połączeniami, synchronizacja stanu, lobby.
* **Plugins**: Zewnętrzne biblioteki (np. Netcode, Mirror).
* **Systems**: Globalne menedżery (GameManager, SaveSystem, Audio).

### 🛠️ _Features
Logika podzielona na konkretne mechaniki (Moduły).
* **AI**: Zachowania botów i nawigacja.
* **Customization**: System zmiany wyglądu pojazdów.
* **PowerUps**: Logika bonusów na trasie.
* **TrailSystem**: Mechanika śladu i detekcji kolizji.
* **Vehicle**: Fizyka jazdy i obsługa Inputu.

### 📦 _Prefabs
Skonfigurowane obiekty gotowe do użycia na scenach.
* Podzielone analogicznie do systemów (Vehicles, Environment, Networking, VFX).

### 📊 _Data & _Settings
Dane statyczne i konfiguracja.
* **ScriptableObjects**: Dane statystyczne, bazy przedmiotów.
* **Settings**: Profile URP, Input Actions, Physics Settings.

### 🧪 _Debug
Przestrzeń robocza programistów: sceny testowe, cheaty, prototypy mechanik.

### 🖼️ _UI
Interfejs użytkownika: czcionki (TMP), ikony, sprite'y, atlasy.

### 🎬 _Scenes
Oficjalne sceny: Bootstrap (startowa), Menus, Levels.

### 🧩 _Common
Elementy współdzielone (Enums, Constants, Shared Data Models).


