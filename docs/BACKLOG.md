# BACKLOG — Skema pengerjaan (proyek Unity yang sudah ada)

Titik awal: proyek Unity sudah punya **Cesium**, **air**, dan **model objek**. Skema ini memakai ketiganya, tidak membuat ulang. Isi prompt lengkap task T0.4 dan T1.x–T6.x ada di `BACKLOG_ORIGINAL.md`; di sini hanya perubahan (Δ) akibat proyek sudah ada.

## Cara pakai
Satu task per sesi Claude Code, berurutan, di branch `rov-sim/<id>`. Buka setiap sesi dengan:
> Baca CLAUDE.md, docs/STATE.md, docs/SPEC.md, docs/ARCHITECTURE.md. Kerjakan task <ID> dari docs/BACKLOG.md (dan prompt aslinya di BACKLOG_ORIGINAL.md bila disebut). Buat rencana singkat dulu, tunggu persetujuan, lalu kerjakan. Akhiri dengan memperbarui docs/STATE.md.

Label: **AGENT** (Claude Code), **MANUAL** (manusia), **CAMPURAN**. Usaha relatif: S / M / L. Model: Sonnet default; Opus untuk A0.1 bila proyek besar, T3.1, T3.4, T4.2, T6.1.

## Gambaran alur
| Tahap | Task | Hasil |
|---|---|---|
| A. Audit dan adopsi | A0.1 – A0.7, T0.4 | Kondisi proyek terdokumentasi; Cesium, air, model siap dipakai kode simulasi |
| B. Kendaraan | T1.1 – T1.4 | Tortuga bisa dikemudikan di air nyata |
| C. Lingkungan dan sensor | T2.1 – T2.5 | Demo 1: ROV kecil lengkap |
| D. Skenario dan instruktur | T3.1 – T3.4 | Sesi dua layar, gangguan, station keeping |
| E. Rekam dan debrief | T4.1 – T4.4 | Demo 2: sesi lengkap sampai laporan |
| F. Paralel (manusia) | T5.1, T5.2 | UI sesuai desain, aset final |
| G. Work class dan lanjutan | W1 – W3, T6.x | ECA Hytech, Mariner XL, jaringan, uji performa |

---

## A. Audit dan adopsi

### A0.1 Audit proyek [AGENT, S] — kerjakan PERTAMA, hanya membaca
**Prompt:** Jangan mengubah file apa pun selain `docs/AUDIT.md` dan `docs/STATE.md`. Audit proyek Unity ini dan tulis hasilnya:
1. Versi Unity, render pipeline (HDRP/URP/Built-in), isi `Packages/manifest.json` (versi Cesium, Input System, Test Framework, dsb.).
2. Cesium: scene mana yang memakainya, objek `CesiumGeoreference`, `Cesium3DTileset` (sumber: ion/URL/lokal), koordinat asal, apakah ada collider fisika pada tileset, apakah tile bisa dimuat tanpa internet.
3. Air: solusi apa (HDRP Water System, shader kustom, asset pihak ketiga), apakah punya ketinggian permukaan yang bisa dibaca skrip, efek bawah air (kabut/post-process) dan bagaimana dipicu.
4. Model: daftar FBX/prefab ROV, lokasi, jumlah vertex, material/tekstur yang terpasang, apakah tekstur lengkap (BaseColor, Normal, Metallic/Roughness).
5. Struktur folder, asmdef yang ada, scene di Build Settings, status git dan `.gitignore`.
6. Compile error/warning di Console dan masalah yang terlihat (shader pink, tekstur hilang, dsb.).
7. Daftar risiko untuk rencana ini dan pertanyaan yang perlu dijawab manusia.
Isi tabel di `docs/STATE.md`. Bila ada yang tidak bisa diketahui dari file, tulis `[tidak diketahui — perlu dicek manual]`, jangan menebak.
**Selesai bila:** `docs/AUDIT.md` ada, tabel STATE terisi, tidak ada file lain berubah (`git status` bersih selain dua dokumen).

### A0.2 Baseline git dan cadangan [MANUAL, S]
Commit kondisi saat ini ke branch `main`, buat tag `baseline-sebelum-rov-sim`. Pastikan `.gitignore` Unity (Library, Temp, Logs, obj, UserSettings), aktifkan Git LFS untuk `*.fbx`, `*.png`, `*.jpg`, `*.exr`, tekstur Cesium cache. Salin proyek ke cadangan di luar repo. Daftarkan Unity MCP bila belum.
**Selesai bila:** tag ada, clone bersih bisa dibuka Unity tanpa error.

### A0.3 Adopsi Cesium: dasar laut offline [CAMPURAN, M]
Pengganti T0.2; memakai setup Cesium yang sudah ada.
**Prompt:** Buat scene `Sim` dari scene Cesium yang ada (duplikat, jangan ubah aslinya). (a) Baca URL tileset dari `StreamingAssets/config.json`, tidak ditulis keras. (b) Fallback: bila server tidak terjangkau dalam 5 detik, pakai terrain prosedural lokal 2 km × 2 km. (c) Pastikan dasar laut punya collider fisika (aktifkan physics mesh pada tileset bila perlu) agar ROV tidak menembus dan sonar raycast mengenai dasar. (d) Atur titik asal (`CesiumGeoreference`) di lokasi uji dan sediakan `SeabedQuery.HeightAt(x, z)` di `Environment`. (e) Tulis `docs/SPIKE_CESIUM.md`: yang berhasil, yang gagal, cara menyiapkan tile server lokal.
**Manual:** sediakan data batimetri contoh (GEBCO/data Pushidrosal bila ada), jalankan tile server lokal, matikan internet saat uji.
**Selesai bila:** dengan internet mati dasar laut termuat dari server lokal; fallback aktif bila server mati; raycast ke bawah mengenai dasar; keputusan tertulis di STATE.

### A0.4 Adopsi air: lapisan lingkungan [AGENT, M]
**Prompt:** Bungkus air yang ada di `IWaterEnvironment` (ARCHITECTURE bagian 5): `SurfaceHeightAt(x, z)`, `DepthAt(position)`, `Current(position, time)` (nol dulu), `Visibility(depth)`. Visual air tidak diubah. Bila air hanya visual (tidak ada ketinggian yang dapat dibaca), pakai bidang datar y=0 untuk fisika dan sinkronkan dengan visual. Tambahkan pemicu bawah air berbasis posisi kamera: ganti kabut/post-process, warna menurut kedalaman. Jangan mengubah material air; bila perlu perubahan, tulis usulan di STATE.
**Selesai bila:** EditMode test untuk `SurfaceHeightAt`/`DepthAt`; kamera yang turun melewati permukaan memicu efek bawah air dan naik mematikannya; tidak ada perubahan pada aset air.

### A0.5 Catat keputusan pipeline [CAMPURAN, S]
Pengganti T0.3. Pipeline sudah terpasang, jadi tidak ada spike pindah pipeline.
**Prompt:** Tambahkan `FpsLogger` (rata-rata, p95, min FPS selama 60 detik ke CSV) dan jalankan di scene `Sim` dengan air, Cesium, dan 20 objek.
**Manual:** ukur di PC target atau setara, isi `docs/DECISION_PIPELINE.md`. Bila p95 FPS di bawah 30, catat sebagai risiko dan jangan pindah pipeline tanpa persetujuan.
**Selesai bila:** angka FPS tercatat dan keputusan "tetap di <pipeline>" ada di STATE.

### A0.6 Struktur dan asmdef tanpa memindah aset [AGENT, S]
**Prompt:** Buat `Assets/_Falah/` dengan asmdef sesuai ARCHITECTURE bagian 2. Tambahkan package yang belum ada (Input System, Test Framework). Buat scene `Boot`, `Menu`, `Debrief`, tambahkan ke Build Settings. Aset yang sudah ada tetap di tempatnya; kode baru yang perlu referensi ke sana lewat prefab atau `Resources`/Addressables, bukan memindah file. Catat perintah menjalankan test di CLAUDE.md.
**Selesai bila:** compile bersih, satu EditMode test dummy lolos, tidak ada aset lama berpindah (cek `git status`: hanya file baru).

### A0.7 Impor dan prefab model ROV [CAMPURAN, M]
Ikuti `docs/MODEL_INTEGRATION.md`.
**Prompt:** Untuk Tortuga dan ECA Hytech: buat prefab sesuai struktur anak di dokumen, collider primitif, titik thruster kosong, kamera, dan `RovTexturePacker` untuk material sesuai pipeline di STATE. Mariner XL dan "Teledyne" cukup prefab placeholder bertanda jelas. Jangan mengubah FBX asli. Tulis daftar masalah model (poligon, tekstur, pivot, skala) di STATE.
**Manual:** LOD/retopologi, tekstur ulang Mariner XL, model Teledyne yang benar.
**Selesai bila:** prefab Tortuga tampil benar (tekstur lengkap, tanpa pink) di scene `Sim`, di dalam air, dengan ≥ 60 FPS pada satu ROV; daftar masalah model tercatat.

### T0.4 Profil ROV [AGENT, M]
Sama dengan `BACKLOG_ORIGINAL.md`. Δ: profil menunjuk ke prefab dari A0.7 (field `visualPrefab`); posisi thruster di profil harus cocok dengan Transform `Thrusters/Txx` di prefab, validator editor memeriksanya.

---

## B. Kendaraan

### T1.1 Penyelaman dasar [AGENT, M]
Prompt asli. Δ: pakai prefab Tortuga dari A0.7 (bukan kotak), air dari `IWaterEnvironment` A0.4, dasar laut dari A0.3 sebagai collider. Tambahkan tes: ROV dijatuhkan dari permukaan lalu mendarat di dasar tanpa menembus.

### T1.2 Thruster dan alokasi kontrol [AGENT, L]
Prompt asli, tanpa perubahan. Thruster dibaca dari profil; visual propeller opsional dan hanya dikerjakan bila prefab punya anak thruster.

### T1.3 Input dan kamera [AGENT, M]
Prompt asli. Δ: kamera bawah air memakai efek air dari A0.4; pastikan kamera ROV tidak keluar dari volume air tanpa efek permukaan.

### T1.4 Telemetri dan HUD minimal [AGENT, S]
Prompt asli.

**Gerbang B:** ROV Tortuga dapat dikemudikan di air dengan Cesium sebagai dasar; ulangi tes offline.

## C. Lingkungan dan sensor

### T2.1 Arus dan visibilitas [AGENT, M]
Prompt asli. Δ: arus disuntikkan lewat `IWaterEnvironment.Current` dari A0.4; visibilitas mengubah parameter kabut air yang ada lewat satu komponen pengendali (tidak mengubah material).

### T2.2 Tether [AGENT, M] — prompt asli.
### T2.3 Lingkungan dasar laut dan target [CAMPURAN, M]
Prompt asli. Δ: target (pipa, jangkar, kotak) ditaruh di atas dasar Cesium memakai `SeabedQuery.HeightAt`, bukan terrain terpisah.
### T2.4 Sonar [AGENT, M] — prompt asli (raycast ke collider dasar dari A0.3).
### T2.5 Demo 1 [CAMPURAN, S]
Prompt asli. Pilot menguji Tortuga di dasar laut dari tile lokal, internet mati.

## D. Skenario dan instruktur
T3.1 Skenario dan objektif [AGENT, M, Opus]; T3.2 Station instruktur [CAMPURAN, L]; T3.3 Gangguan thruster [AGENT, S]; T3.4 Station keeping DP [AGENT, L, Opus]. Semua prompt asli. Aturan mode hanya Latihan dan Ujian (SPEC); abaikan referensi empat mode di dokumen lama bila ada.

## E. Rekam dan debrief
T4.1 Perekam [AGENT, M]; T4.2 Pemutar ulang [AGENT, L, Opus]; T4.3 Layar debrief [CAMPURAN, M]; T4.4 Demo 2 [CAMPURAN, S]. Prompt asli. Angka penilaian tetap `[__]` sampai disepakati dengan Pushidrosal.

## F. Paralel oleh manusia
T5.1 Gaya UI dan tema [MANUAL, M]: terapkan desain (token warna dan font di SPEC). T5.2 Aset ROV dan lingkungan [MANUAL, L]: lanjutan dari MODEL_INTEGRATION (LOD, tekstur ulang, model Teledyne benar).

## G. Work class dan lanjutan
- W1 Profil dan prefab ECA Hytech [AGENT+MANUAL, M]: setelah varian dikonfirmasi; thruster dan massa dari data resmi.
- W2 Panel manipulator, TMS/LARS [AGENT, L]: lengan dengan joint, HPU, tension tether.
- W3 Profil dan prefab Mariner XL [AGENT+MANUAL, M]: setelah tekstur ulang dan data dikonfirmasi; skenario angkat.
- T6.1 Jaringan trainee-instruktur [AGENT, L, Opus]; T6.2 Uji performa mesin target [MANUAL, S].

## Definisi selesai prototype
Sama dengan `BACKLOG_ORIGINAL.md`, ditambah: seluruh uji lolos dengan internet mati; tidak ada aset awal yang hilang atau berubah tanpa tercatat di STATE; setiap task punya entri di Log.

## Aturan gerbang antar tahap
Sebelum pindah tahap (B→C, dst.) manusia mereview diff, menjalankan scene `Sim`, dan merge ke `main`. Bila gerbang gagal, perbaiki di tahap itu, jangan lanjut.
