# Throne - struktura projektu

Ten projekt jest projektem Unity. Aktualna struktura trzyma zasoby gry w `Assets/Project`, zewnętrzne paczki w `Assets/Plugins`, a konfigurację Unity w standardowych folderach `Packages` i `ProjectSettings`.

## Główne katalogi

```text
Assets/
├── Plugins/
│   ├── FishNet/
│   │   └── FishNet.Config.XML
│   ├── ParrelSync/
│   └── Suggo Creations/
└── Project/
    ├── Art/
    │   ├── Audio/
    │   ├── Lighting/
    │   ├── Materials/
    │   ├── Models/
    │   ├── Textures/
    │   └── UI/
    ├── Data/
    │   ├── Animation/
    │   ├── Networking/
    │   ├── Renderer/
    │   ├── Settings/
    │   ├── URPDefaultResources/
    │   └── Vehicle/
    ├── Prefabs/
    │   ├── BackRock-NeonCity/
    │   ├── Environment/
    │   ├── Networking/
    │   ├── PowerUps/
    │   ├── UI/
    │   └── Vehicles/
    ├── Scenes/
    │   ├── Arenas/
    │   └── UI/
    └── Scripts/
        ├── AreaBoundary/
        ├── Config/
        ├── Editor/
        ├── GameFlow/
        ├── GameSession/
        ├── Menu/
        ├── Networking/
        ├── PowerUps/
        ├── TrailSystem/
        └── Vehicle/
```

## Zasady organizacji

`Assets/Project` jest miejscem na kod i zasoby należące do gry. Nowe skrypty, sceny, prefaby, modele, materiały i dane projektu powinny trafiać do odpowiedniego podfolderu tutaj.

`Assets/Plugins` jest miejscem na zewnętrzne biblioteki i assety dostawców. Aktualnie znajdują się tam FishNet, ParrelSync oraz Suggo Creations. Kod gry nie powinien być dokładany bezpośrednio do tych folderów, chyba że jest to świadoma modyfikacja paczki.

`Assets/Plugins/FishNet/FishNet.Config.XML` przechowuje konfigurację FishNet, w tym ścieżkę do generowanej kolekcji prefabów sieciowych: `Assets/Project/Data/Networking/DefaultPrefabObjects.asset`.

## Opis folderów `Assets/Project`

### Art

Zasoby wizualne, dźwiękowe i importowane assety:

- `Audio` - efekty dźwiękowe i muzyka menu.
- `Lighting` - dane oświetlenia dla środowisk.
- `Materials` - materiały Unity, w tym materiały aren i paczki BackRock-NeonCity.
- `Models` - modele FBX i powiązane pliki źródłowe, w tym pojazdy, ringi, power-upy i elementy Neon City.
- `Textures` - tekstury, pliki `.fbm` oraz mapy używane przez modele i materiały.
- `UI` - grafiki interfejsu, ikony, zasoby menu oraz TextMesh Pro.

### Data

Dane konfiguracyjne i assety techniczne:

- `Animation` - klipy i dane animacji.
- `Networking` - dane FishNet, w tym `DefaultPrefabObjects.asset`.
- `Renderer` - assety renderera i dane związane z renderowaniem środowisk.
- `Settings` - ustawienia gry i Unity.
- `URPDefaultResources` - zasoby domyślne Universal Render Pipeline.
- `Vehicle` - dane konfiguracyjne pojazdów.

### Prefabs

Gotowe obiekty Unity używane na scenach:

- `BackRock-NeonCity` - prefaby środowiska Neon City, budynki, ulice, park, elementy dekoracyjne i efekty.
- `Environment` - prefaby areny, granic mapy i środowiska.
- `Networking` - prefaby startowe i sterujące trybem multiplayer.
- `PowerUps` - prefaby bonusów rozgrywki.
- `UI` - prefaby menu, lobby i leaderboardu.
- `Vehicles` - prefaby pojazdów, warianty botów, preview i lusterka.

### Scenes

Oficjalne sceny projektu:

- `Arenas` - sceny rozgrywki: `Neon City`, `Neon City XL`, `Neon City XL Multiplayer`, `Neon City XXL`.
- `UI/Menu` - `MainMenu` i `MultiplayerConnection`.
- `UI/Lobby` - `SingleplayerLobby` i `MultiplayerLobby`.
- `UI` - `GameOver`.

### Scripts

Kod gry podzielony według funkcji:

- `AreaBoundary` - logika granic areny.
- `Config` - ScriptableObjecty i ustawienia gry, botów oraz wyglądu gracza.
- `Editor` - rozszerzenia edytora Unity, obecnie dla systemu śladu i życia pojazdu.
- `GameFlow` - timer, start meczu, koniec gry i spawn pointy.
- `GameSession` - inicjalizacja i runtime sesji meczu.
- `Menu` - obsługa menu, lobby, wyboru pojazdu, wyników, ustawień i przejść scen.
- `Networking` - bootstrap multiplayer, stan meczu i driver sesji FishNet.
- `PowerUps` - logika power-upów i spawnera.
- `TrailSystem` - emisja śladu, segmenty, kolor pojazdu i sekwencja śmierci.
- `Vehicle` - sterowanie graczem i botami, komendy wejścia, kamera, ruch i synchronizacja kół.

## Pliki i foldery Unity poza `Assets`

- `Packages/manifest.json` - zależności Unity Package Manager.
- `ProjectSettings/` - ustawienia projektu Unity.
- `UserSettings/`, `Library/`, `Temp/`, `Logs/` - lokalne foldery Unity, nie są częścią ręcznie utrzymywanej struktury źródeł.
