# ARCHITECTURE — ROV Trainer Simulator

## 1. Prinsip
- Satu platform, banyak ROV: perbedaan ROV hanya ada di `RovProfile` (data) dan prefab visual.
- Simulasi terpisah dari tampilan: logika fisika dan sensor tidak tahu apa-apa soal UI.
- Semua yang bisa diuji ditulis sebagai kelas murni C#.
- Alur data satu arah: Input → Controller → Physics → Sensors → Telemetry → UI/Recorder.

## 2. Struktur folder dan asmdef
```
Assets/
  _Project/
    Core/         Falah.RovSim.Core        profil, interface, event bus, tipe data. Tidak mereferensi modul lain.
    Rov/
      Profiles/   (aset RovProfile: Tortuga, Teledyne)
      Physics/    Falah.RovSim.Physics     thruster, drag, buoyancy, tether, DP
      Sensors/    Falah.RovSim.Sensors     kamera, depth, heading, IMU, sonar
      Input/      Falah.RovSim.Input       peta kontrol, pembaca gamepad/joystick
    Environment/  Falah.RovSim.Environment arus, visibilitas, kolom air, integrasi Cesium
    Scenario/     Falah.RovSim.Scenario    data skenario, objektif, gangguan
    Recording/    Falah.RovSim.Recording   perekam, pemutar ulang, format file
    Network/      Falah.RovSim.Network     (P6) sinkronisasi trainee-instruktur
    UI/           Falah.RovSim.UI          layar, HUD, station instruktur, debrief
    Art/          model, material, tekstur, efek
    Scenes/       Boot, Menu, Sim, Debrief
  Tests/
    EditMode/     Falah.RovSim.Tests.EditMode
    PlayMode/     Falah.RovSim.Tests.PlayMode
```
Arah dependensi: `UI → (Scenario, Recording, Environment, Sensors, Physics, Input) → Core`. `Physics` boleh memakai `Environment` lewat interface di `Core` (`IWaterEnvironment`).

## 3. Tipe inti (Core)
- `RovProfile : ScriptableObject` — lihat bagian 4.
- `IControlInput` — `Vector3 Translation (surge, sway, heave)`, `float Yaw`, `float Pitch`, `bool HoldHeading, HoldDepth, HoldPosition`, `float LightLevel`, `float CameraTilt`. Sumber: gamepad, joystick, atau skrip uji.
- `IWaterEnvironment` — `Vector3 CurrentAt(Vector3 worldPos)`, `float VisibilityAt(float depth)`, `float WaterSurfaceY`, `float Density`.
- `ITelemetrySource` — menghasilkan `TelemetrySample` (waktu, posisi, rotasi, depth, heading, kecepatan, status thruster, mode DP).
- `SimEvent` (struct) — `time, type, payload`. Tipe: `SessionStart, ObjectiveDone, ThrusterFault, CurrentChanged, Collision, DpEngaged, SessionEnd`.
- `SimClock` — sumber waktu sesi tunggal (bukan `Time.time` langsung), agar replay bisa memakai waktu rekaman.

## 4. RovProfile (data per ROV)
| Grup | Field | Catatan |
|---|---|---|
| Identitas | `id`, `displayName`, `class` (Inspection/WorkClass), `visualPrefab` | |
| Massa | `mass`, `centerOfMass`, `centerOfBuoyancy`, `volume`, `inertiaTensorScale` | COB di atas COM memberi momen pemulih |
| Drag | `linearDragCoeff (Vector3)`, `quadraticDragCoeff (Vector3)`, `angularDrag (Vector3)`, `addedMassFactor` | Drag kuadratik per sumbu |
| Thruster | daftar `ThrusterDef`: `position`, `direction`, `maxForwardThrust`, `maxReverseThrust`, `responseTime`, `groupTag` | Arah dalam ruang lokal; azimuth boleh diubah lewat sudut |
| Allocation | `controlMapping` | Matriks pengalokasi dihitung otomatis dari `ThrusterDef` (pseudo-inverse) |
| Tether | `tetherLength`, `tetherDiameter`, `tetherDragCoeff`, `tetherMassPerMeter` | Prototype: tether sederhana |
| Sensor | `cameras[]` (posisi, FOV, tilt), `sonar` (range, beam, resolusi, update rate), `depthNoise`, `headingNoise` | |
| DP | `headingPid`, `depthPid`, `positionPid` (Kp, Ki, Kd, batas integral, batas output) | Hasil tuning di T3.4 |
| Operasi | `maxDepth`, `maxSpeed`, `batteryOrPowerNotes` | `maxDepth` jadi batas keras |
| Status data | `dataStatus` (Confirmed / Estimated / Placeholder) | UI debug menampilkan peringatan bila bukan Confirmed |

Nilai awal (semua PLACEHOLDER, wajib dikonfirmasi):
- **Tortuga**: kelas inspeksi, 4 thruster azimuth, mode DP, braket sonar. Kedalaman maks publik ~500 m `[perlu konfirmasi]`. Massa `[___]`, dimensi `[___]`.
- **Teledyne**: kelas inspeksi/LBV, kemungkinan seri SeaBotix (4 thruster vektor + 2 vertikal) `[model perlu konfirmasi]`. Massa `[___]`.
Jangan mengisi angka dari ingatan model. Pakai nilai sementara yang masuk akal secara fisika, tandai `dataStatus = Placeholder`.

## 5. Model fisika (modul Physics)
Semua di `FixedUpdate`, fixed timestep 0.01 s. Satu `Rigidbody` per ROV, gravitasi aktif, `useGravity` + gaya apung manual.

1. **Thruster**: `ThrusterModel` (kelas murni). Perintah kontrol → vektor wrench 6-DOF → alokasi ke thrust per thruster (pseudo-inverse dari matriks konfigurasi, dengan saturasi proporsional). Respons orde pertama dengan `responseTime`. Gaya diterapkan dengan `AddForceAtPosition` di posisi thruster.
2. **Drag**: `F = -(c1·v + c2·|v|·v)` pada kecepatan relatif air (v_rov − v_arus), per sumbu lokal. Drag rotasi serupa dengan angular velocity.
3. **Daya apung**: gaya ke atas `ρ·g·V` di titik COB (diterapkan dengan `AddForceAtPosition`). Fraksi volume terendam dihitung dari kedalaman terhadap permukaan (halus di dekat permukaan). Ini menghasilkan momen pemulih roll dan pitch.
4. **Massa tambahan**: pendekatan lewat `addedMassFactor` pada drag dan penskalaan inertia. Jangan membuat solver baru.
5. **Arus**: `IWaterEnvironment.CurrentAt` — mulai dari vektor konstan + gangguan berkala; nanti profil terhadap kedalaman.
6. **Tether**: tahap 1 = rantai segmen verlet untuk visual, ditambah gaya tarik pada ROV `F = f(panjang terulur, arus, drag tether)`. Tahap lanjut (di luar prototype) = ArticulationBody/joint.
7. **DP (station keeping)**: `DpController` (kelas murni) dengan tiga PID: heading, depth, posisi horizontal (dalam frame ROV). Keluaran = wrench tambahan ke alokasi thrust. Aktivasi per mode lewat `IControlInput`.
8. **Kegagalan**: `ThrusterFault(thrusterId, efficiency 0..1)` mengurangi `maxThrust` thruster tertentu pada waktu sesi.

Stabilitas: gunakan `Rigidbody.interpolation = Interpolate`, batas kecepatan dan batas gaya, dan uji unit untuk memastikan tidak ada NaN.

## 6. Sensor (modul Sensors)
- **Kamera**: Camera Unity dengan kabut volumetrik sederhana; FOV dan tilt dari profil. Pencahayaan ROV (lampu) mengikuti `LightLevel`.
- **Depth/heading/IMU**: dibaca dari Rigidbody, ditambah noise Gauss kecil dari profil.
- **Sonar** (prototype): raycast kipas (horizontal) 10 Hz, hasil jarak menjadi buffer intensitas dan tampilan 2D. Array raycast dialokasikan sekali (`RaycastCommand` bila perlu performa). Implementasi GPU di luar prototype.

## 7. Lingkungan dan Cesium (modul Environment)
- Paket: Cesium for Unity. Sumber data dasar laut: tile quantized-mesh atau 3D Tiles, **dilayani server lokal** (bukan Cesium ion cloud). Alternatif yang dievaluasi di T0.2: cesium-terrain-builder, Cesium ion Self-Hosted.
- URL sumber tile dibaca dari file konfigurasi `StreamingAssets/config.json`, bukan ditulis keras.
- Bila tile server tidak tersedia: fallback terrain prosedural lokal supaya simulator tetap jalan (untuk uji dan demo).
- Permukaan air, kolom air, kabut, dan warna menurut kedalaman: shader/volume sesuai render pipeline yang diputuskan di T0.3.

## 8. Skenario (modul Scenario)
`ScenarioDef : ScriptableObject` — titik awal, posisi target sonar, kondisi lingkungan awal, daftar `ObjectiveDef` (tipe, parameter, batas waktu), daftar `DisturbanceDef` yang bisa disisipkan instruktur. `ScenarioRunner` mengevaluasi objektif dan memancarkan `SimEvent`.

## 9. Rekaman dan replay (modul Recording)
- Rekam `TelemetrySample` pada 20 Hz dan semua `SimEvent`, ke file biner/JSON di `Application.persistentDataPath/sessions/`.
- Header file: versi format, `rovId`, `scenarioId`, waktu mulai, versi aplikasi.
- Replay = memutar ulang pose (kinematik), bukan mensimulasi ulang. Sederhana dan tidak bergantung pada determinisme fisika.
- Pemutar: play, pause, seek, kecepatan. Penanda kejadian pada timeline.
- Format file harus berversi dari awal (perubahan format di tahap lanjut tidak boleh merusak rekaman lama).

## 10. UI (modul UI)
- Teknologi: UI Toolkit (Unity 6). Satu `UIDocument` per layar. Gaya dari USS bersama (token warna sama dengan desain: latar `#0A101A`, panel `#101828`, aksen `#4DB8E8`, peringatan `#F5B342`; font Barlow dan IBM Plex Mono).
- Layar: Menu (pilih ROV, skenario), HUD simulasi, Station instruktur, Debrief.
- Desain acuan: artifact "Desain Aplikasi ROV Simulator Pushidrosal". Ikuti tata letak dan label, bukan piksel persis.
- Angka telemetri pada HUD memakai font monospace, lebar tetap.

## 11. Pengujian
- EditMode: alokasi thrust (kasus tiap sumbu, saturasi, thruster mati), PID (step response, anti-windup), drag dan buoyancy (nilai bentuk tertutup), format rekaman (tulis lalu baca = sama), penilaian objektif.
- PlayMode: ROV jatuh ke kedalaman keseimbangan, ROV tidak bergerak di air tenang tanpa input, DP menahan posisi dengan arus konstan dalam toleransi, sesi 5 menit tanpa NaN atau pelanggaran batas.
- Uji manual (MANUAL): rasa kendali oleh 2 orang selain developer; catat di `docs/PLAYTEST.md`.
