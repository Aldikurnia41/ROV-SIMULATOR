# AUDIT — proyek Unity `D:\RoV Simulator` (task A0.1)

Tanggal: 2026-10-07. Hanya membaca, kecuali catatan "Perubahan sebelum audit" di bawah. Sumber: file proyek dan Unity MCP (instance `RoV Simulator`, scene `RoVGameplay`). Yang tidak bisa dipastikan ditulis `[tidak diketahui — perlu dicek manual]`.

## Perubahan sebelum audit (WAJIB DIBACA)
Sebelum CLAUDE.md dibaca, scene `RoVGameplay` sudah diubah dalam sesi lain (selaras dengan keputusan "pindah ke perairan dangkal Kalimantan"). Ini melanggar aturan "catat nilai lama dulu". Nilai lama dan cadangan:
- Cadangan file scene sebelum diubah: `D:\cesium ion\backup\RoVGameplay.unity.bak-20261007`.
- `CesiumGeoreference` origin: lama lon 118.24994498, lat -0.75001919, h 0 → baru 117.97, -0.50, 0.
- `Water System` (anak CesiumGeoreference), local position: lama (0, -800, 3) → baru (0, 0, 3).
- 10 objek `suitcase` dipindah ke dasar laut (raycast ke MeshCollider terrain). Lama → baru (x dan z tetap):
  | Objek | y lama | y baru |
  |---|---|---|
  | suitcase | -243.28 | -246.01 |
  | suitcase (1) | -237.07 | -230.86 |
  | suitcase (2) | -89.90 | -222.32 |
  | suitcase (3) | -242.56 | -204.71 |
  | suitcase (4) | -233.43 | -210.38 |
  | suitcase (5) | -152.33 | -232.36 |
  | suitcase (6) | -87.84 | -229.74 |
  | suitcase (7) | -96.27 | -228.90 |
  | suitcase (8) | -192.86 | -230.08 |
  | suitcase (9) | -191.96 | -229.29 |
- `Ferry` tidak berubah (-153.2). `Manager` tidak berubah.
- File baru: 2 PNG di `Assets/Screenshots/`. Opsi Console "Error Pause" dimatikan sementara lalu dikembalikan ke aktif.
- Tidak ada perubahan pada `LocalTileset` (URL sudah ada sebelumnya), material air, atau manifest.

## 1. Versi dan paket
- Unity **6000.0.53f1**; render pipeline **URP 17.0.4** (`GraphicsSettings` memakai asset URP kustom; bukan HDRP).
- Paket: Cesium for Unity **1.25.1**, Input System 1.14.0, Test Framework 1.5.1, uGUI 2.0.0, Splines 2.8.4, Timeline 1.8.7, AI Navigation 2.0.8, Visual Scripting 1.9.7, Unity MCP (CoplayDev, `#main` tidak dikunci versi).
- Package UI Toolkit tidak terpasang sebagai paket terpisah (bagian bawaan editor Unity 6).
- Aset non-paket: Zenject (Assets/Plugins), KriptoFX Water System, WaterWorks, AllSkyFree, TextMesh Pro.

## 2. Cesium
- Satu scene memakainya: `RoVGameplay` (satu-satunya scene di Build Settings). `RoVGameplay KWS` dan `SampleScene` tidak diperiksa isinya.
- `CesiumGeoreference` + `CesiumCameraManager` (satu objek), tiga anak: `LocalTileset`, `Cesium World Bathymetry` (nonaktif), `Water System`.
- `LocalTileset`: sumber **From Url** `http://192.168.101.39:8080/kalimantan/tileset.json` (tile server nginx di PC pengembang, bukan ion cloud). Data: ETOPO 2022 30 detik busur, Selat Makassar. `createPhysicsMeshes = true` (MeshCollider aktif, sudah terbukti raycast mengenai dasar).
- `Cesium World Bathymetry` (ion, aset 2426648) nonaktif, memakai server `ion.cesium.com`.
- Aset `CesiumIonServer` `cesium_local` berisi URL tileset sebagai server URL → request `GET /appData` ke nginx dijawab 404.
- Termuat tanpa internet: **ya**, selama tile server di 192.168.101.39:8080 hidup (pesan "Cannot connect" ke `api.cesium.com` muncul tetapi tidak menghalangi). Bergantung pada IP DHCP; belum ada `StreamingAssets/config.json` (ARCHITECTURE bagian 7).
- Resolusi data ±925 m per sel: cukup untuk konteks regional, bukan detail skala ROV.

## 3. Air
- Solusi: **KriptoFX Water System** (objek `Water System`; folder `Assets/KriptoFX/WaterSystem`, 138 MB, asmdef `KWS_asmdef`) dan folder `WaterWorks` (demo). Material air tidak diubah.
- Ketinggian permukaan terbaca skrip: **tidak ada API dari skrip proyek**; konvensi di kode: y=0 adalah permukaan (`RoVPhysics` `position.y < 0` terendam; `HudBinder` depth = -y; `UnderwaterEffect` depth = -y kamera). Apakah KWS menggeser permukaan sendiri: `[tidak diketahui — perlu dicek manual]`.
- Efek bawah air: `UnderwaterEffect.cs` (warna menurut kedalaman, kabut, intensitas cahaya `exp(-0.05·depth)`), `FogDisabler.cs`. Pemicunya berbasis posisi y kamera.

## 4. Model
| Model | File | Ukuran |
|---|---|---|
| ECA Hytech | `3D Contents/ECA HYTECH/tripo_convert_349d…fbx` | 3,0 MB |
| Mariner L | `3D Contents/ROV MARINER L/ROV.fbx` | 0,2 MB |
| Mariner XL | `3D Contents/ROV MARINER XL/tripo_convert_5204…fbx` | 0,7 MB |
| Tortuga | `3D Contents/ROV TORTUGA/tripo_convert_9382…fbx` | 3,1 MB |
| "Teledyne" | `3D Contents/TELEDYNE/tripo_convert_bf93…fbx` | 2,8 MB (menurut MODEL_INTEGRATION: glider Slocum, bukan ROV) |
| Sea-sar | `3D Contents/SEALION/sea-sar.fbx` | 0,1 MB |
| Lain | `Ferry.fbx` (15,3 MB), `suitcase.fbx`, `CC_AD5` (NPC), `particle.prefab` | |
- Di scene: `Hytech` aktif (RoVPhysics, Rigidbody, BoxCollider), `Mariner L` dan `sea-sar-1` nonaktif.
- Jumlah vertex dan kelengkapan tekstur (BaseColor/Normal/Metallic/Roughness): `[tidak diketahui — perlu dicek manual]` (MODEL_INTEGRATION menyebut ±300 ribu vertex per model, Mariner XL ±60 ribu).

## 5. Struktur
- Folder Assets: `3D Contents`, `AllSkyFree`, `CesiumSettings`, `Ferry`, `KriptoFX`, `Plugins`, `Prefabs`, `Resources`, `Scenes`, `Screenshots`, `Scripts`, `Settings`, `TextMesh Pro`, `TutorialInfo`, `UI`, `WaterWorks`. Belum ada `Assets/_Falah/`, `StreamingAssets/`, folder `Tests`.
- Skrip proyek (10, tanpa asmdef): `CableFollowHead`, `FogDisabler`, `HudBinder`, `Installer`, `Propeller`, `RoVPhysics`, `RovViewTrigger`, `SearchScenarioTracker`, `SwitchRoV`, `UnderwaterEffect`. Memakai Zenject (`SceneContext`, `Installer`).
- UI yang ada: **uGUI** (3 Canvas: `CanvasROV`, `CanvasROV_Instrument` dengan `HudBinder`, `CanvasInstructorOperating`), aset gambar `Assets/UI/*.png` (attitude, compass, depth, frame).
- asmdef: hanya `KWS_asmdef` di luar Plugins; 13 asmdef di `Assets/Plugins` (Zenject).
- Scene di Build Settings: hanya `Assets/Scenes/RoVGameplay.unity`. Scene lain: `RoVGameplay KWS`, `SampleScene`, demo Zenject/KriptoFX/WaterWorks.
- **Git: tidak ada** (tidak ada `.git` maupun `.gitignore`; `D:\rov-sim` juga bukan repo). `docs/` belum ada di proyek Unity (paket dokumen masih di `D:\rov-sim`).

## 6. Console
- Error/warning kode: tidak ada compile error terlihat. Pesan Editor `Couldn't create ... UnityDirMonSyncFile` (3x, path instalasi Unity; tidak berasal dari proyek).
- Runtime Cesium: "Failed to obtain ion server application data: 404", "Exception while resuming Cesium ion connection", "Request for api.cesium.com/appData failed: Cannot connect". Warning fisika: segitiga >500 unit pada MeshCollider terrain.
- **"Error Pause" Console aktif** → pesan Cesium di atas mem-pause Play mode (hanya 3 frame berjalan). Perlu keputusan manusia.
- Shader pink/tekstur hilang: `[tidak diketahui — perlu dicek manual]`.

## 7. Risiko dan pertanyaan
Risiko:
1. Tidak ada git: aturan CLAUDE.md (branch per task, working tree bersih) tidak bisa dijalankan; belum ada cadangan resmi (A0.2).
2. UI saat ini uGUI; ARCHITECTURE menetapkan UI Toolkit (Unity 6). Desain artifact tidak sama dengan SPEC (lihat STATE, "Pertanyaan terbuka").
3. Error Cesium ion mem-pause Play; tile server bergantung IP LAN DHCP.
4. Data batimetri ±925 m; objek target dulunya bertingkat kedalaman, kini hampir sama (dasar laut lebih datar).
5. Paket Unity MCP tidak dikunci versi; port MCP berubah antar sesi (bukan 49160 tetap).
Pertanyaan: lihat `docs/STATE.md` bagian "Pertanyaan terbuka untuk manusia".
