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

| 2026-10-07 | Menu (Login dummy, Pilih ROV, Skenario, Briefing) dibangun dengan uGUI di scene `Assets/_Falah/Scenes/Menu.unity` oleh `Falah/UI/Build Menu Scene`; token warna dari desain; font masih TMP default (IBM Plex belum diimpor) | Keputusan user: login dummy, gaya mengikuti desain, tetap uGUI, T5.1 boleh lewat skrip |
| 2026-10-07 | Cakupan prototype di UI: hanya Tortuga + Teledyne yang bisa dipilih; mode Misi penuh + Ujian; skenario Investigasi target sonar. Item lain tampil terkunci ("Fase berikut") | SPEC bagian 2-3 (desain memuat 4 ROV/4 mode/5 skenario) |
| 2026-10-07 | Canvas menu memakai Screen Space - Camera (bukan Overlay); `PlayerSettings.runInBackground = true`; Build Settings: Menu jadi scene 0, RoVGameplay scene 1 | Agar menu bisa di-capture/diuji otomatis dan Play tetap jalan saat Editor tidak fokus |

| 2026-10-07 | HUD pilot (desain 5a) ditambahkan ke `RoVGameplay` sebagai canvas baru `CanvasHUD_Pilot` (Overlay, Display 1, sorting 5). `CanvasROV/Header` dan `CanvasROV/Compass` dinonaktifkan (nilai lama: activeSelf = true) karena digantikan HUD baru; `CanvasROV/Button` (Capture, dipakai `SearchScenarioTracker`), `CanvasROV_Instrument` (+`HudBinder`) dan `CanvasInstructorOperating` TIDAK diubah | Keputusan user: sesuaikan HUD. Kembalikan dengan mengaktifkan lagi dua objek itu dan menghapus `CanvasHUD_Pilot` |
| 2026-10-07 | Font IBM Plex Sans/Mono (OFL) diimpor ke `Assets/_Falah/UI/Fonts` dan dipakai di menu dan HUD | Keputusan user |

| 2026-10-07 | Station Instruktur = canvas `CanvasInstructorStation` (Overlay, Display 0, sorting 10) di `RoVGameplay`; tampil otomatis bila peran Instruktur/Administrator, F2 menampilkan/menyembunyikan, F10 mengakhiri sesi. `SessionController` (objek baru) memegang jam sesi, sampling jejak 1 Hz, dan pindah ke scene `Debrief` | Keputusan agent berdasarkan SPEC (satu mesin, dua layar). Display 0 sebelumnya hanya `CanvasInstructorOperating` + kamera third person (tidak diubah) |
| 2026-10-07 | Debrief = scene baru `Assets/_Falah/Scenes/Debrief.unity`; Build Settings: Menu, RoVGameplay, Debrief. Laporan JSON (berversi) disimpan di `Application.persistentDataPath/sessions/`. Skor tetap `[__]` (bobot belum disepakati); Ekspor PDF dan Putar ulang dikunci | SPEC bagian 2-3, BACKLOG E |

| 2026-10-07 | `Assets/Scripts/RoVPhysics.cs` (skrip proyek lama) diubah minimal untuk T3.3: `using Falah.RovSim.Core`, gaya naik/turun dikali `HeaveEfficiency`, gaya maju/mundur dikali `SurgeEfficiency`, RPM propeller dikali efisiensi tiap thruster; 2 properti + 1 helper ditambahkan. Perilaku tanpa gangguan identik (efisiensi = 1) | T3.3 butuh pengait ke gaya thruster; `ThrusterModel` (T1.2) belum ada. Pemetaan thruster mengikuti komentar di kode: 0-1 surge, 2-3 heave; sway belum terikat thruster |

| 2026-10-07 | Modul `Assets/_Falah/Scenario` (asmdef `Falah.RovSim.Scenario`, refs Core). `ScenarioDef`/`ObjectiveDef` berupa kelas C# biasa (bukan ScriptableObject) dan skenario dasar didefinisikan di `ScenarioLibrary`; jadikan ScriptableObject bila perlu diedit di Inspector. Target dicari lewat nama objek scene (`suitcase` = target pertama di RoVGameplay) oleh `SceneTargetLocator` | T3.1; posisi target milik scene, jadi tetap ikut snap ke dasar laut |
| 2026-10-07 | `SearchScenarioTracker` (pelacak lama, Zenject, tombol Capture) TIDAK diubah dan berjalan berdampingan dengan `ScenarioRunner` baru | Aturan jangan refactor yang tidak terkait; penggabungan keduanya perlu keputusan |

| 2026-10-08 | `ThrusterModel` (Core, kelas murni) + `ThrusterLayouts` + `RovThrusterDriver` (Integration, `AddForceAtPosition`). Driver BELUM dipasang di ROV scene: `RoVPhysics` tetap menggerakkan Hytech; pemasangan/pemetaan input = T1.3 | T1.2; menghindari dua sistem gaya ganda pada satu Rigidbody |

## Status task
| Task | Status | Catatan |
|---|---|---|
| A0.1 | selesai (2026-10-07) | `docs/AUDIT.md` dan tabel di atas terisi. Satu pelanggaran proses dicatat (perubahan scene sebelum audit) |
| A0.2 | sebagian | Git diinisialisasi dan remote tersambung; commit baseline + tag + cadangan BELUM (menunggu user; repo GitHub publik, ada aset berlisensi pihak ketiga) |
| A0.6 | sebagian | `Assets/_Falah/{Core,UI,UI/Editor,Tests/EditMode}` + asmdef; scene `Menu` di Build Settings. Belum: `Boot`/`Debrief` scene, asmdef modul lain |
| T5.1 UI (layar 1-4) | selesai (2026-10-07) | Login, Pilih ROV, Skenario, Briefing. Alur diuji di Play: login kosong ditolak, pilihan masuk `SessionSetup`, ECA/Mariner terkunci, Mulai simulasi aktif setelah kalibrasi lalu memuat `RoVGameplay`. 9/9 tes EditMode lulus |
| T1.4 HUD pilot (layar 5a) | selesai (2026-10-07) | Data nyata: kedalaman, ketinggian dari dasar (raycast), heading, pitch/roll (via `RoVTelemetryAdapter` -> `ITelemetrySource`), timer, arus (SessionSetup), banner peringatan (`PilotHud.ShowAlert`). Data contoh (tag "Data contoh"): sonar, thruster azimuth, tether, lampu, tilt kamera, mode DP. 30/30 tes EditMode lulus |
| T3.2 Station instruktur (layar 6) | selesai (2026-10-07) | Live: sesi/rekaman, jeda, akhiri sesi, injeksi 5 gangguan (banner HUD pilot langsung), arus/arah/visibilitas (HUD ikut), catatan, linimasa kejadian, peta jejak ROV, tampilan trainee (kamera dari ROV aktif). BELUM: efek gangguan dan lingkungan pada fisika/visual (T3.3, T2.1); target di peta |
| T4.3 Debrief (layar 7) | selesai (2026-10-07) | Kejadian, durasi, catatan, jejak (ruas gangguan kuning), penanda di bar putar ulang, simpan laporan JSON. BELUM: skor (bobot), putar ulang (T4.2), PDF (di luar scope), rekaman persisten 20 Hz (T4.1) |
| T3.3 Gangguan thruster | selesai (2026-10-07) | `ThrusterFaultModel` (Core): kebocoran #3 turun linear ke batas bawah, thruster mati #1 = 0; dipakai fisika (`RoVPhysics`) dan dial HUD. 48/48 tes lulus. Di Play: efisiensi heave 1.00 -> 0.70, HUD T3 9.3 -> 3.7 (kuning). Parameter `LeakFloorEfficiency` 0.4 dan `LeakDecaySeconds` 30 adalah PLACEHOLDER (menunggu RovProfile dan data nyata). Gaya fisik end-to-end tidak terukur otomatis (injeksi input Editor gagal) - perlu uji manual dengan tombol naik/turun |
| T3.1 Skenario dan objektif | selesai (2026-10-07) | `ObjectiveDef` (ReachZone, HoldPosition, Identify), `ScenarioRunner` (berurutan, batas waktu per objektif, event `ObjectiveDone`/`ObjectiveFailed`/`ScenarioComplete`), `ScenarioDriver` di `SessionController`. Skenario "Investigasi target sonar" dari SPEC 5. Instruktur melihat baris Objektif; Debrief menampilkan "Objektif x dari y"; laporan JSON memuat jumlah objektif. 58/58 tes lulus. Di Play: 3 objektif selesai berurutan (00:03, 00:20, 00:23). Toleransi (radius 40 m; tahan 12 m / 10 dtk; kenali 10 m / 3 dtk / sudut 25 derajat) adalah PLACEHOLDER |
| T1.2 Thruster dan alokasi | selesai (2026-10-08) | Alokasi pseudo-inverse teredam (6-DOF), saturasi proporsional (arah wrench terjaga), respons orde pertama, efisiensi/mati dari `ThrusterFaultModel` (thruster mati dikeluarkan dari alokasi, sisanya mengompensasi). Tes per sumbu (surge, sway, heave, yaw) untuk Tortuga dan Teledyne, saturasi, thruster mati, respons: 79/79 tes lulus. Play: tiap perintah satu sumbu menggerakkan hanya sumbu itu untuk kedua profil. Geometri/gaya layout = PLACEHOLDER (Tortuga: 3 horizontal + 1 vertikal; Teledyne: 4 vektor + 2 vertikal) sampai RovProfile (T0.4) dan data resmi |
| T5.1 UI (layar 5b) | ditunda | HUD Mariner XL (fase 5, di luar scope prototype) |

## Log sesi
- 2026-10-07 (2): git init + remote; docs disalin ke proyek; cesium_local dihapus. Sisa error: `Cannot connect api.cesium.com` (jaringan ke server Cesium tidak terjangkau dari PC ini) masih muncul pada resume sesi ion default; 404 /appData sudah hilang. Play mode Editor sempat berhenti (butuh fokus jendela Unity).
- 2026-10-07: A0.1 audit dikerjakan. Desain UI di artifact "ROV Trainer Pushidrosal: Desain UI" dibaca (8 layar: Login, Pilih ROV, Mode/Skenario/Lingkungan, Briefing, HUD Tortuga, HUD Mariner XL, Instruktur, Debrief). Belum ada kode UI.

- 2026-10-07 (3): GitHub: baseline `baf710a` + tag `baseline-sebelum-rov-sim` di-push ke origin/main. Branch `rov-sim/t5.1-ui-menu`: menu 4 layar + tes. Error Pause dimatikan (keputusan user).

- 2026-10-07 (4): Station Instruktur + Debrief + `SessionLog`/`SessionReport` (Core) + 41 tes EditMode lulus. Branch `rov-sim/t3.2-instructor-debrief`.

- 2026-10-07 (5): T3.3 gangguan thruster (PR #2) dan T3.1 skenario/objektif. Branch `rov-sim/t3.1-scenario-objectives`.

## Pertanyaan terbuka untuk manusia
0c. Toleransi objektif skenario (radius zona, waktu tahan, jarak/sudut identifikasi) belum dikonfirmasi; nilai sekarang placeholder.
0d. Setelah semua objektif selesai sesi belum berakhir otomatis (instruktur/F10 yang mengakhiri). Perlu pindah otomatis ke Debrief?
0e. Mode Ujian (tanpa petunjuk) belum membedakan tampilan; HUD belum menampilkan objektif ke trainee.
0. Skor Debrief: kriteria dan bobot (Navigasi, Station keeping, Penanganan gangguan, Penyelesaian misi, Waktu) belum disepakati dengan Pushidrosal; tampil `[__]`.
0b. Batas waktu skenario "Investigasi target sonar" masih `[___]` menit (timer menampilkan `--:--`).
0b. `TileServerStatus` memakai URL tile server yang tertulis di komponen (IP LAN); pindahkan ke `StreamingAssets/config.json` pada A0.3.
0c. Lanjut ke layar 5-8 (HUD, Instruktur, Debrief): HUD lama (uGUI + `HudBinder`) sudah ada di scene RoVGameplay; apakah HUD baru menggantikannya?
1. Commit baseline + tag + push ke GitHub: belum dilakukan (menunggu perintah user).
2. Konfirmasi lisensi model Tripo (`3D Contents`) sebelum dimasukkan ke repo.
