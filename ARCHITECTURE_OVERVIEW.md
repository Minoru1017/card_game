# Project Architecture Overview

> **DataManager / PlayerData / scene flow** (single-page overview)  
> Implementation anchors: `PlayerData.ResolveCanonical()`, canonical `DeckManager`, `SceneLoader`, `GlobalNavRuntime`, `HallSceneFeatureBinder`

```mermaid
flowchart TB
    subgraph PERSIST["Disk persistence (Application.persistentDataPath)"]
        PD_CSV["playerdata.csv<br/>active_slot · player slots slot,1..3<br/>deck_slot_name · deckslot · card · profile_*"]
        PROF_CSV["player_profile.csv<br/>match summary · uuid · role"]
    end

    subgraph DDOL["DontDestroyOnLoad — cross-scene singletons"]
        direction TB
        DM["DataManager<br/>(Prefab / scene instance)"]
        PD["PlayerData<br/>★ ResolveCanonical — sole read/write"]
        DMGR["DeckManager<br/>★ canonical sceneLoaded/Unloaded hooks"]
        CS["CardStore<br/>CardList.csv"]
        GNR["GlobalNavRuntime<br/>≡ menu · player info overlay"]

        DM --> PD
        DM --> DMGR
        DM --> CS
    end

    subgraph SCENES["Scenes (UI recreated on each LoadScene)"]
        LOGIN["login<br/>sign-in · pick/create player slot"]
        HALL["hall<br/>home hub"]
        BUILD["Buildbeck<br/>deck build · rename · save deck"]
        PERS["Persistent<br/>backpack / hub"]
        STORE["CardStore<br/>pack open · shop"]
        SET["Settings<br/>battle UI scale · quality"]
        BATTLE["BattleSimulation<br/>turn battle · weather · AI"]
        PLOT["MainPlot<br/>story (if enabled)"]
    end

    subgraph HELPERS["Per-scene helpers (spawned with scene)"]
        SL["SceneLoader<br/>EnterBattle · EnterPersistent"]
        BINDER["HallSceneFeatureBinder<br/>hall buttons → scenes"]
        BBB["BuildbeckLayoutAutoBinder<br/>rewire deck tabs/buttons"]
        BSM["BattleSimulationManager<br/>reads selectedDeckSlot deck"]
    end

    %% --- Data flow ---
    PD <-->|"LoadPlayerData / SavePlayerData"| PD_CSV
    GNR -->|"RefreshProfileFromRuntime<br/>Save first, then SyncProfile"| PD
    GNR --> PROF_CSV

    CS -->|"GetCardById / LoadCardData"| PD
    DMGR -->|"EnsureCoreRefs → PlayerData"| PD
    BSM -->|"LoadPlayerData → GetDeckMap(selectedDeckSlot)"| PD

    %% --- Boot & navigation ---
    LOGIN -->|"set active_slot"| PD
    LOGIN --> HALL

    HALL --> BINDER
    BINDER -->|"Deck"| BUILD
    BINDER -->|"Backpack"| PERS
    BINDER -->|"Shop"| STORE

    GNR -->|"Home"| HALL
    GNR -->|"Backpack"| PERS
    GNR -->|"Settings"| SET
    GNR -->|"Login"| LOGIN
    GNR -.->|"Player info overlay<br/>(no scene change)"| PD

    BUILD --> SL
    BUILD --> BBB
    BBB --> DMGR
    SL -->|"return / EnterPersistent"| PERS
    SL -->|"ready → preview → EnterBattle"| BATTLE

    PERS --> HALL
    PERS --> BUILD
    STORE --> PD

    %% --- Buildbeck lifecycle ---
    DMGR -->|"unload Buildbeck<br/>sceneUnloaded → SavePlayerData"| PD
    DMGR -->|"load Buildbeck<br/>rewire UI → LoadPlayerData → refresh labels"| BUILD

    %% Styles
    classDef core fill:#2d5016,stroke:#1a3009,color:#fff
    classDef scene fill:#1e3a5f,stroke:#0f1f33,color:#fff
    classDef disk fill:#5c4a1f,stroke:#3d3014,color:#fff
    classDef nav fill:#4a2c5c,stroke:#2e1a3a,color:#fff

    class PD,DM,DMGR core
    class LOGIN,HALL,BUILD,PERS,STORE,SET,BATTLE,PLOT scene
    class PD_CSV,PROF_CSV disk
    class GNR nav
```

---

## Legend

| Block | Meaning |
|-------|---------|
| **DDOL** | `DataManager` survives scene loads; `PlayerData` owns save data; only the canonical `DeckManager` registers global scene callbacks |
| **Scenes** | Scene UI is destroyed on switch; entering Buildbeck requires `BuildbeckLayoutAutoBinder` + `CoReloadBuildbeckDeckUi` to rebind controls |
| **playerdata.csv** | Deck display names (`deck_slot_name`), five deck slots (`deckslot`), coins, collection, etc. See [DECK_SAVE_IMPLEMENTATION.md](./DECK_SAVE_IMPLEMENTATION.md) |

## Main scene routes

| From | Action | To |
|------|--------|-----|
| login | Sign-in success | hall |
| hall | Deck | Buildbeck |
| hall | Backpack | Persistent |
| hall | Shop | CardStore |
| Buildbeck | Back | Persistent (`SceneLoader.EnterPersistent`) |
| Buildbeck | Battle ready | BattleSimulation (via preview modal) |
| Any (≡ menu) | Home / Settings / Login | hall / Settings / login |
| Any (≡ menu) | Player info | **Overlay** (same scene); triggers `SavePlayerData` |

## Save / load timing (summary)

- **Write**: save deck, switch deck slot, confirm rename, leave Buildbeck, open player info, `PlayerProfileCsvService.RefreshProfileFromRuntime`
- **Read**: `PlayerData.Awake`, Buildbeck UI reload, hall resource bar, `EnterBattle` (forces disk read before battle)
- **Avoid stale overwrite**: after rename / save deck, call `SceneLoader.RefreshEnterBattleState(false)`

## Related docs

- [DIFFICULTY_AND_AI_DESIGN.md](./DIFFICULTY_AND_AI_DESIGN.md) — battle difficulty tiers and enemy AI (report chapter)
- [ENEMY_AI_DECISION_TREE.md](./ENEMY_AI_DECISION_TREE.md) — detailed play decision tree
