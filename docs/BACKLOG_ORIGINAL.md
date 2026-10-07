# BACKLOG — Prototype ROV Trainer Simulator

Cara pakai: kerjakan task **berurutan**, satu task per sesi Claude Code. Mulai sesi dengan perintah:
> Baca CLAUDE.md, docs/SPEC.md, docs/ARCHITECTURE.md. Kerjakan task <ID> dari docs/BACKLOG.md. Buat rencana singkat dulu.

Label: **AGENT** = dikerjakan Claude Code. **MANUAL** = dikerjakan manusia (Claude hanya menulis daftar langkah). **CAMPURAN** = keduanya; Claude menyelesaikan bagian kode lalu menyerahkan daftar langkah manual.
Estimasi usaha relatif: S / M / L (bukan hari).

Rekomendasi model: Sonnet untuk sebagian besar task. Opus hanya untuk T3.1, T3.4, T4.2, T6.x (perancangan dan debug lintas file). Hindari loop panjang pada file .cs yang besar dengan Opus.

---

## P0 — Persiapan dan keputusan teknis

### T0.1 Buat proyek dan kerangka [CAMPURAN, S]
**Prompt:** Buat struktur folder dan asmdef sesuai ARCHITECTURE bagian 2 di proyek Unity yang sudah ada. Tambahkan package: Input System, Test Framework, Cesium for Unity (dari Git URL resmi). Buat scene kosong Boot, Menu, Sim, Debrief dan masukkan ke Build Settings. Catat perintah menjalankan test di CLAUDE.md.
**Manual (kamu):** buat proyek Unity baru (template sesuai keputusan T0.3 sementara, boleh HDRP), inisialisasi git dengan `.gitignore` Unity, daftarkan Unity MCP.
**Selesai bila:** proyek compile bersih, semua asmdef ada, satu EditMode test dummy lolos, scene masuk build.

### T0.2 Spike: tile server lokal dan Cesium [CAMPURAN, M]
**Prompt:** Buat scene `Spike_Cesium`. Tampilkan satu tileset dasar laut dari URL yang dibaca dari `StreamingAssets/config.json`. Tambahkan fallback: bila URL tidak terjangkau dalam 5 detik, pakai terrain prosedural lokal (Perlin, 2 km x 2 km). Tulis `docs/SPIKE_CESIUM.md` berisi langkah menyiapkan tile server lokal (cesium-terrain-builder atau Cesium ion Self-Hosted), apa yang berhasil, apa yang gagal.
**Manual (kamu):** sediakan satu data batimetri contoh (format apa pun yang tersedia; bila belum ada, pakai data terbuka seperti GEBCO) dan jalankan tile server di mesin lokal.
**Selesai bila:** dengan internet dimatikan, dasar laut termuat dari server lokal; fallback aktif bila server mati; dokumen spike ada dengan keputusan pendekatan.

### T0.3 Spike: HDRP vs URP [CAMPURAN, S]
**Prompt:** Buat scene uji dengan kolom air sederhana (kabut, warna menurut kedalaman), terrain, 20 objek, satu kamera. Tambahkan komponen `FpsLogger` yang mencatat rata-rata, p95, dan min FPS selama 60 detik ke file CSV. Siapkan skrip editor untuk mengganti pipeline.
**Manual (kamu):** jalankan di PC target atau setara untuk HDRP dan URP, isi tabel hasil di `docs/DECISION_PIPELINE.md`.
**Selesai bila:** ada angka FPS kedua pipeline di mesin setara target dan keputusan tertulis.

### T0.4 Format profil ROV dan dua profil placeholder [AGENT, M]
**Prompt:** Implementasikan `RovProfile` dan tipe pendukungnya di `Core` sesuai ARCHITECTURE bagian 4. Buat dua aset profil `Tortuga` dan `Teledyne` dengan nilai placeholder (komentar `// PLACEHOLDER`, `dataStatus = Placeholder`). Tambahkan validator editor (thruster tanpa arah, massa nol, volume nol, PID negatif) yang menampilkan peringatan.
**Selesai bila:** dua aset profil ada; EditMode test validator lolos; tidak ada angka spesifik yang diklaim sebagai data resmi.

---

## P1 — Fondasi kendaraan

### T1.1 Penyelaman dasar: ROV, air, daya apung [AGENT, M]
**Prompt:** Implementasikan `RovBody` (Rigidbody dari profil), `BuoyancyModel`, `DragModel`, dan `IWaterEnvironment` sederhana (permukaan di y=0, arus nol). Buat scene `Sim` dengan prefab ROV kotak placeholder yang dibangun dari profil. Fixed timestep 0.01.
**Selesai bila:** PlayMode test: ROV dijatuhkan dari permukaan mencapai keseimbangan sesuai massa/volume profil; ROV diam di air tenang tanpa input; tidak ada NaN dalam 60 detik.

### T1.2 Thruster dan alokasi kontrol [AGENT, L]
**Prompt:** Implementasikan `ThrusterModel` (kelas murni) dengan alokasi pseudo-inverse, saturasi proporsional, respons orde pertama, dan dukungan `ThrusterFault`. Hubungkan ke `RovBody` lewat `AddForceAtPosition`. Tulis EditMode test untuk tiap sumbu (surge, sway, heave, yaw), saturasi, dan satu thruster mati.
**Selesai bila:** test lolos; perintah satu sumbu menghasilkan gerak dominan pada sumbu itu untuk kedua profil.

### T1.3 Input dan kamera [AGENT, M]
**Prompt:** Implementasikan `IControlInput` dengan pembaca gamepad via Input System (peta kontrol di aset Input Actions), plus `ScriptedInput` untuk test. Kamera ROV (FOV/tilt dari profil), kamera orbit untuk debug (toggle). Deadzone dan kurva respons dapat diatur.
**Manual (kamu):** uji dengan gamepad, tulis kesan awal di `docs/PLAYTEST.md`.
**Selesai bila:** ROV dapat dikendalikan dengan gamepad; mengganti profil mengubah perilaku dan posisi thruster.

### T1.4 Telemetri dan HUD minimal [AGENT, S]
**Prompt:** Implementasikan `TelemetrySample` dan `ITelemetrySource` di ROV. Buat HUD UI Toolkit minimal: depth, heading, kecepatan, status DP, peringatan. Font monospace untuk angka.
**Selesai bila:** HUD menampilkan nilai benar dan bergerak halus; tidak ada alokasi per frame pada pembaruan HUD (cek Profiler).

---

## P2 — Lingkungan, tether, sensor (ROV kecil lengkap)

### T2.1 Arus dan visibilitas [AGENT, M]
**Prompt:** Implementasikan `WaterEnvironment` dengan arus (vektor konstan + gangguan perlahan) dan visibilitas terhadap kedalaman. Kontrol dapat diubah saat berjalan lewat API (untuk instruktur). Hubungkan drag memakai kecepatan relatif air.
**Selesai bila:** PlayMode test: ROV tanpa input hanyut searah arus dengan kecepatan akhir sesuai drag; mengubah visibilitas mengubah kabut kamera.

### T2.2 Tether sederhana [AGENT, M]
**Prompt:** Implementasikan tether sebagai rantai segmen verlet (visual) dan gaya tarik pada ROV sesuai ARCHITECTURE 5.6. Panjang dan diameter dari profil. Titik tambat di satu titik tetap pada scene. Jangan membuat simulasi joint.
**Selesai bila:** tether terlihat mengikuti ROV dan terpengaruh arus; ROV tertahan di batas panjang tether tanpa jitter atau NaN selama 5 menit.

### T2.3 Lingkungan dasar laut dan target [CAMPURAN, M]
**Prompt:** Integrasikan hasil T0.2 ke scene `Sim`: dasar laut dari Cesium (dengan fallback), tabrakan ROV dengan dasar laut, dan satu objek target (placeholder). Pancarkan `SimEvent.Collision`.
**Manual (kamu):** tentukan posisi target dan titik awal di data skenario.
**Selesai bila:** ROV bisa mendarat di dasar laut dan memicu event tabrakan; scene berjalan tanpa internet.

### T2.4 Sonar sederhana [AGENT, M]
**Prompt:** Implementasikan sonar raycast kipas 10 Hz (parameter dari profil) dan tampilan sonar 2D di HUD. Alokasikan array sekali; tanpa GC per frame. Target di scene muncul sebagai kontak.
**Selesai bila:** kontak target terlihat pada sonar pada jarak dan arah benar; Profiler menunjukkan tidak ada alokasi GC dari sonar.

### T2.5 Demo 1: ROV kecil siap [CAMPURAN, S]
**Prompt:** Buat menu sederhana pilih Tortuga atau Teledyne lalu masuk ke `Sim`. Tulis `docs/DEMO1.md`: langkah demo, apa yang berfungsi, apa yang placeholder.
**Manual (kamu):** playtest dua orang; catat temuan di `docs/PLAYTEST.md`.
**Selesai bila:** dari menu bisa memilih salah satu ROV dan menyelam, perbedaan perilaku terasa; daftar temuan playtest ada.

---

## P3 — Station keeping, skenario, instruktur

### T3.1 Skenario dan objektif [AGENT, M]  (Opus untuk rancangan)
**Prompt:** Implementasikan `ScenarioDef`, `ObjectiveDef` (tipe: ReachZone, HoldPosition, Identify), `ScenarioRunner` dan `SimEvent`. Buat skenario "Investigasi target sonar" dari SPEC bagian 5. EditMode test untuk evaluasi objektif.
**Selesai bila:** objektif berurutan terdeteksi selesai dengan benar pada test; event tercatat.

### T3.2 Station instruktur [CAMPURAN, L]
**Prompt:** Implementasikan layar instruktur (UI Toolkit) pada jendela/display kedua dalam satu proses: tampilan peta/jalur ROV, daftar objektif, kontrol arus dan visibilitas, tombol sisip gangguan, log kejadian, catatan. Mengacu pada artboard "Instruktur" di desain.
**Manual (kamu):** pengaturan multi-display, penyesuaian tata letak.
**Selesai bila:** instruktur dapat mengubah arus dan visibilitas, menyisipkan gangguan, dan semuanya muncul di log dan telemetri trainee.

### T3.3 Gangguan: kebocoran thruster [AGENT, S]
**Prompt:** Hubungkan `DisturbanceDef` tipe ThrusterFault ke `ThrusterModel`. Efek: efisiensi thruster turun bertahap, peringatan di HUD trainee, event tercatat.
**Selesai bila:** test: thruster yang gagal kehilangan thrust sesuai profil; HUD menampilkan peringatan.

### T3.4 Station keeping (DP) [AGENT, L]  (Opus)
**Prompt:** Implementasikan `PidController` (anti-windup, batas keluaran) dan `DpController` tiga mode (heading, depth, posisi) sesuai ARCHITECTURE 5.7. EditMode test untuk PID (step response, windup). PlayMode test: ROV menahan posisi di arus konstan dalam toleransi `[___]` m. Siapkan `DpTuningWindow` di editor untuk menyetel PID per profil dan memplot respons.
**Manual (kamu):** tuning nilai PID untuk Tortuga dan Teledyne berdasarkan rasa dan toleransi.
**Selesai bila:** DP stabil tanpa osilasi berlebihan untuk kedua profil dalam arus lemah dan sedang; test lolos.

---

## P4 — Rekam, replay, debrief

### T4.1 Perekam [AGENT, M]
**Prompt:** Implementasikan `SessionRecorder` dan format file berversi (ARCHITECTURE 9): header, sampel 20 Hz, event. Tulis ke `persistentDataPath/sessions/`. EditMode test: tulis lalu baca menghasilkan data sama; file versi lama tetap bisa dibaca (buat test dengan fixture).
**Selesai bila:** sesi 5 menit tersimpan; ukuran file wajar; test round-trip lolos.

### T4.2 Pemutar ulang [AGENT, L]  (Opus)
**Prompt:** Implementasikan `SessionPlayer`: memutar pose ROV (kinematik) dari rekaman dengan play, pause, seek, kecepatan, serta penanda event pada timeline. ROV replay tidak boleh memakai fisika. Gunakan `SimClock` agar waktu replay konsisten.
**Selesai bila:** replay menggerakkan ROV dengan jalur sama dengan sesi asli (posisi sama dalam toleransi 1 cm di tiap sampel); seek mundur dan maju tidak merusak status.

### T4.3 Layar debrief [CAMPURAN, M]
**Prompt:** Implementasikan layar Debrief mengacu pada artboard "Debrief": jalur ROV 2D dengan penanda event, timeline interaktif, panel metrik mentah (ketepatan station keeping, waktu, tabrakan, objektif). Skor akhir tampil `[__]` sampai kriteria dikonfirmasi.
**Manual (kamu):** polesan visual.
**Selesai bila:** dari akhir sesi bisa masuk debrief, memutar ulang, dan melihat metrik mentah.

### T4.4 Demo 2 [CAMPURAN, S]
**Prompt:** Hubungkan alur penuh: Menu → pilih ROV → skenario → sim → debrief. Tulis `docs/DEMO2.md`.
**Manual (kamu):** sesi uji utuh 15-20 menit dengan dua orang, catat hasil di `docs/PLAYTEST.md`.
**Selesai bila:** satu sesi penuh berjalan tanpa crash dan replay terlihat sama dengan aslinya.

---

## P5 — Penyelarasan visual (paralel dengan P2-P4 oleh Unity Artist)

### T5.1 Gaya UI dan tema [MANUAL, M]
Unity Artist dan Document & UX menyiapkan USS dan aset sesuai token desain, mengganti placeholder UI.

### T5.2 Aset ROV dan lingkungan [MANUAL, L]
Unity Artist mengganti kotak placeholder dengan model Tortuga dan Teledyne (acuan visual `[___]`), objek target, efek partikel (gelembung, partikel melayang), material kolom air sesuai pipeline terpilih.

---

## P6 — Opsional, setelah prototype stabil

### T6.1 Jaringan trainee-instruktur [AGENT, L]  (Opus)
**Prompt:** Rancang dan evaluasi pendekatan jaringan LAN untuk dua mesin (Netcode for GameObjects atau transport sederhana). Tulis perbandingan di `docs/DECISION_NETWORK.md` sebelum menulis kode. Instruktur hanya menerima telemetri dan mengirim perintah (arus, visibilitas, gangguan).
**Selesai bila:** keputusan tertulis disetujui, lalu implementasi lolos uji dua mesin dalam LAN tanpa internet.

### T6.2 Uji performa pada mesin target [MANUAL, S]
Jalankan di PC latih target, catat FPS dan temuan di `docs/PLAYTEST.md`.

---

## Definisi selesai prototype
- Semua task P0-P4 selesai; T6.x opsional.
- `docs/DEMO1.md`, `DEMO2.md`, `PLAYTEST.md`, `DECISION_PIPELINE.md`, `SPIKE_CESIUM.md` terisi.
- Semua parameter ROV yang belum dikonfirmasi masih bertanda placeholder dan terdaftar di `docs/SPEC.md` bagian 7.
