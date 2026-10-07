# SPEC — Prototype ROV Trainer Simulator

Draft 1 · 6 Oktober 2026 · Software Development Division

## 1. Tujuan prototype
Membuktikan tiga hal sebelum pekerjaan penuh dimulai:
1. Rasa kendali ROV kecil (Tortuga, Teledyne) cukup meyakinkan untuk operator, dan dua ROV terasa berbeda hanya dengan mengganti profil data.
2. Satu sesi latihan utuh bisa berjalan: pilih ROV, pilih skenario, kendalikan, instruktur memberi gangguan, sesi direkam, debrief dengan replay.
3. Pipeline teknis aman: Unity terhubung ke tile server lokal (tanpa internet), dan pilihan render pipeline sudah diuji di perangkat target.

Prototype adalah alat untuk demo internal dan demo 1 ke Pushidrosal. Bukan produk jadi.

## 2. Dalam scope
- Dua profil ROV: Tortuga dan Teledyne. Parameter berupa placeholder sampai dikonfirmasi.
- Fisika kendaraan: thruster, drag, daya apung, arus, tether sederhana, mode station keeping (DP) untuk heading, depth, dan posisi.
- Sensor: kamera utama, depth, heading, IMU dasar, sonar sederhana berbasis raycast.
- Lingkungan: kolom air (warna, kabut, visibilitas menurun terhadap kedalaman), dasar laut dari data lokal (Cesium + tile server lokal), satu objek target.
- Satu skenario: Investigasi target sonar. Objektif: menuju kontak sonar, station keeping, identifikasi objek.
- Station trainee (pilot) dan station instruktur. Prototype awal: satu mesin, dua layar/jendela. Jaringan dua mesin masuk tahap akhir.
- Instruktur dapat mengubah arus, visibilitas, dan menyisipkan satu gangguan (kebocoran thruster).
- Rekam telemetri dan kejadian, putar ulang, tampilan debrief dengan jalur ROV dan penanda kejadian.
- Penilaian: metrik mentah dicatat (ketepatan station keeping, waktu, tabrakan, objektif). Bobot dan skor akhir belum ditentukan, ditampilkan sebagai `[__]`.

## 3. Di luar scope prototype
- ECA Hytech dan Mariner XL, manipulator, TMS, LARS, kapal dukung, gerak kapal.
- Laporan PDF, portal web administrator, akun dan login sungguhan.
- Paket instalasi offline dan UAT. VR.
- Multi-trainee, jaringan lintas mesin (sampai tahap P6, opsional).
Jangan menambah fitur dari daftar ini tanpa persetujuan.

## 4. Persyaratan non-fungsional
- Platform: Windows desktop. Spesifikasi PC latih target: `[___]` (konfirmasi dengan Pushidrosal). Uji di PC setara, bukan hanya mesin developer.
- Target performa awal: 60 FPS pada resolusi 1080p di mesin target `[___]`. Fisika stabil pada fixed timestep 0.01 s.
- Berjalan tanpa internet. Tidak ada panggilan layanan cloud saat runtime.
- Semua data ROV di luar kode (ScriptableObject), supaya ROV kelima tidak perlu mengubah kode inti.
- Teks UI Indonesia, siap lokalisasi.

## 5. Skenario prototype: Investigasi target sonar
1. Mulai dari titik awal, ROV di permukaan atau kedalaman awal `[___]`.
2. Arus lemah, visibilitas sedang.
3. Pilot menyelam, mengikuti sonar menuju kontak (target ada di posisi tersimpan di data skenario).
4. Pilot mengaktifkan station keeping di dekat target.
5. Instruktur menyisipkan kebocoran thruster atau menaikkan arus.
6. Pilot menjaga posisi, mengidentifikasi objek (objektif selesai saat objek berada dalam kamera pada jarak dan waktu tahan tertentu).
7. Sesi berakhir, debrief dengan replay.

## 6. Kriteria sukses prototype
- Dua ROV bisa dipilih dan terasa berbeda (uji lapangan oleh 2 orang selain developer).
- Sesi 15-20 menit berjalan tanpa crash. Replay sama dengan sesi asli secara visual.
- Unity memuat dasar laut dari tile server lokal dengan internet dimatikan.
- Keputusan HDRP atau URP tercatat berdasarkan angka FPS di mesin target.

## 7. Hal yang masih perlu dikonfirmasi
| Hal | Perlu untuk | Pemilik |
|---|---|---|
| Model pasti Teledyne dan ECA Hytech | Parameter profil | Pushidrosal |
| Spesifikasi PC latih | Uji performa | Pushidrosal |
| Format dan sumber data batimetri | T0.2 | Pushidrosal |
| Kriteria penilaian dan bobot | Tahap penilaian | Pushidrosal, Document & UX |
| Pendekatan jaringan trainee-instruktur | P6 | Unity development |
| HDRP atau URP | Seluruh aset visual | Unity dev dan artist (uji T0.3) |
