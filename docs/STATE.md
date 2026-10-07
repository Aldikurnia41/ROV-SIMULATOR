# STATE — kondisi terkini proyek

Diperbarui di akhir setiap sesi Claude Code. Jangan dihapus; tambahkan baris baru di Log.

## Fakta proyek (isi dari A0.1, 2026-10-07; detail di `docs/AUDIT.md`)
| Item | Nilai |
|---|---|
| Versi Unity | 6000.0.53f1 |
| Render pipeline | URP 17.0.4 |
| Cesium for Unity | versi 1.25.1 |
| Sumber tile saat ini | lokal: `http://192.168.101.39:8080/kalimantan/tileset.json` (nginx di PC pengembang, ETOPO 2022 ±925 m). Ion cloud nonaktif. |
| Solusi air | KriptoFX Water System (`Assets/KriptoFX/WaterSystem`) |
| Air punya fisika/ketinggian permukaan? | Tidak ada API dari skrip proyek; konvensi y=0 = permukaan di `RoVPhysics`/`HudBinder`/`UnderwaterEffect`. Perilaku KWS sendiri: [tidak diketahui — perlu dicek manual] |
| Dasar laut punya collider? | Ya (`createPhysicsMeshes`, MeshCollider; raycast terbukti kena) |
| Scene yang ada | `RoVGameplay` (Build Settings), `RoVGameplay KWS`, `SampleScene`, demo plugin |
| Lokasi model ROV | `Assets/3D Contents/{ECA HYTECH, ROV MARINER L, ROV MARINER XL, ROV TORTUGA, TELEDYNE, SEALION}` |
| Package lain yang relevan | Zenject (Plugins), Input System 1.14.0, Test Framework 1.5.1, Splines, Timeline, Unity MCP (CoplayDev) |
| Compile error/warning awal | Tidak ada compile error. Runtime: error Cesium ion (404 /appData, api.cesium.com) + warning fisika triangle >500. Error Pause aktif. |
| Server Cesium ion | Aset `cesium_local` DIHAPUS (lihat Keputusan); `LocalTileset.ionServer` = `ion.cesium.com` (sumber tetap From Url). |
| UI yang ada | uGUI: `CanvasROV`, `CanvasROV_Instrument` (+`HudBinder`), `CanvasInstructorOperating` |
| Git | `git init` (branch main) + remote `origin` = https://github.com/Aldikurnia41/ROV-SIMULATOR.git (repo GitHub PUBLIC, kosong). `.gitignore` + `.gitattributes` (LFS) dibuat. BELUM ada commit/push. |

## Keputusan
| Tanggal | Keputusan | Alasan |
|---|---|---|
| 2026-10-07 | Lokasi latihan sementara: 117.97E, -0.50N (paparan Mahakam, dasar laut ±-241 m); air di y=0 | Skrip memakai y=0 sebagai permukaan, HUD depth 0-400 m. Dikerjakan sebelum CLAUDE.md dibaca; nilai lama di AUDIT.md bagian "Perubahan sebelum audit" |
| 2026-10-07 | Aset `CesiumIonServer` `cesium_local` dihapus; `LocalTileset.ionServer` diganti ke `ion.cesium.com` | Nilai lama: serverUrl = apiUrl = `http://192.168.101.39:8080/kalimantan/tileset.json`, oauthClientId 381 (URL tileset salah dipakai sebagai server ion → `GET /appData` 404). Cadangan: `D:/cesium ion/backup/cesium_local.asset.bak` (+ .meta.bak) |
| 2026-10-07 | Keputusan user: login dummy; token gaya = yang di desain artifact (IBM Plex Sans/Mono, `#08101C`/`#0E1829`/`#38B6F0`/`#F4B13E`); UI tetap uGUI; agent boleh mengerjakan T5.1 lewat skrip; error sumber Cesium ion dibersihkan | Instruksi user sesi ini |

| 2026-10-07 | Aset berlisensi dikeluarkan dari repo (`.gitignore`): KriptoFX, WaterWorks, AllSkyFree, Easy performant outline, `3D Contents` (model Tripo). Zenject (MIT) tetap di repo. README.md mencatat aset yang harus diimpor terpisah | Keputusan user; repo GitHub publik |
| 2026-10-07 | Console Error Pause dimatikan | Keputusan user; error jaringan Cesium (`Cannot connect api.cesium.com`) tidak lagi mem-pause Play |

## Status task
| Task | Status | Catatan |
|---|---|---|
| A0.1 | selesai (2026-10-07) | `docs/AUDIT.md` dan tabel di atas terisi. Satu pelanggaran proses dicatat (perubahan scene sebelum audit) |
| A0.2 | sebagian | Git diinisialisasi dan remote tersambung; commit baseline + tag + cadangan BELUM (menunggu user; repo GitHub publik, ada aset berlisensi pihak ketiga) |
| T5.1 / T1.4 / T3.2 / T4.3 (UI) | belum | Menunggu keputusan di "Pertanyaan terbuka" |

## Log sesi
- 2026-10-07 (2): git init + remote; docs disalin ke proyek; cesium_local dihapus. Sisa error: `Cannot connect api.cesium.com` (jaringan ke server Cesium tidak terjangkau dari PC ini) masih muncul pada resume sesi ion default; 404 /appData sudah hilang. Play mode Editor sempat berhenti (butuh fokus jendela Unity).
- 2026-10-07: A0.1 audit dikerjakan. Desain UI di artifact "ROV Trainer Pushidrosal: Desain UI" dibaca (8 layar: Login, Pilih ROV, Mode/Skenario/Lingkungan, Briefing, HUD Tortuga, HUD Mariner XL, Instruktur, Debrief). Belum ada kode UI.

## Pertanyaan terbuka untuk manusia
1. Commit baseline + tag + push ke GitHub: belum dilakukan (menunggu perintah user).
2. Konfirmasi lisensi model Tripo (`3D Contents`) sebelum dimasukkan ke repo.
