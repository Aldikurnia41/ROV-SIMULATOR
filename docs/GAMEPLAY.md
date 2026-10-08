# Panduan Gameplay — ROV Trainer Simulator

Untuk peserta latih (operator ROV). Nilai teknis (radius, waktu, batas kedalaman) masih PLACEHOLDER sampai dikonfirmasi Pushidrosal.

## 1. Alur sesi
1. Login (dummy) → pilih peran, ROV, mode, skenario → briefing → mulai.
2. Di simulasi, HUD pilot menampilkan kedalaman, ketinggian dari dasar, heading, pitch/roll, arus, sonar, dial thruster, dan batang LAMPU.
3. Sesi berakhir saat **F10** ditekan (atau instruktur mengakhiri) → layar debrief berisi linimasa, jejak, dan ringkasan.

## 2. Kontrol

| Fungsi | Keyboard | Gamepad / joystick |
|---|---|---|
| Maju / mundur | W / S | stik kiri atas/bawah |
| Geser kiri / kanan | A / D | stik kiri kiri/kanan |
| Naik / turun | E / Q | stik kanan atas/bawah (joystick: hat) |
| Putar (yaw) | Panah kanan / kiri | stik kanan kiri/kanan (joystick: twist) |
| **Lampu nyala/mati** | **L** | tombol west |
| **Kecerahan lampu naik / turun** | **T / G** | — |
| **Tandai posisi** | **M** | tombol north |
| Kamera tilt naik / turun | R / F | d-pad atas/bawah |
| Kamera orbit (debug) | F3 (klik kanan geser, scroll zoom) | — |
| Tahan posisi (DP) | C | — |
| Tahan kedalaman | X | — |
| Tahan heading | Z | — |
| Tampilkan/sembunyikan stasiun instruktur | F2 | — |
| Akhiri sesi | F10 | — |

Keyboard dan gamepad bisa dipakai bersamaan; nilainya dijumlahkan.

## 3. Lampu
- Batang **LAMPU** di HUD menunjukkan keluaran nyata (0 % bila mati atau rusak).
- Kecerahan diatur 10–100 % dengan langkah 10 %.
- Di bawah sekitar 40 m tidak ada cahaya matahari: objek hanya bisa diidentifikasi bila lampu menyala (minimal 30 %).
- Instruktur dapat menyuntik "Lampu padam": lampu mati paksa sampai gangguan dicabut. Bila terjadi, naik ke perairan lebih dangkal atau tunggu instruktur memulihkan.

## 4. Skenario: Pencarian black box
Perangkat perekam jatuh di dasar laut di dalam area pencarian. Waktu 45 menit.

1. **Menuju area pencarian** — dekati kontak sonar sampai ±40 m. Pakai sonar (kipas 120°, jangkauan 30 m) dan heading di HUD. Perhitungkan arus.
2. **Identifikasi visual** — dalam ±8 m dari black box, arahkan kamera ke objek (sudut ≤ 25°) dan tahan 3 detik. Di bawah 40 m, nyalakan lampu (L) dan pastikan kecerahan ≥ 30 %. Bilah progres tampil di linimasa.
3. **Tandai posisi** — dalam ±8 m dari objek, tekan **M**. Tekan dari jarak jauh tidak dihitung.
4. **Kembali ke permukaan** — naik sampai kedalaman ≤ 5 m.

Tips:
- Hentikan gerak saat identifikasi; gunakan DP (C) untuk menahan posisi melawan arus.
- Visibilitas rendah memperpendek jarak pandang: dekati pelan.
- Menyentuh dasar laut sambil bergerak mencatat event **Collision** di debrief. Jaga ketinggian > 1 m saat bermanuver.

## 5. Skenario: Investigasi target sonar
Menyelam ke kontak sonar (±40 m) → tahan posisi 10 detik dalam ±12 m → identifikasi visual 3 detik dalam ±10 m.

## 6. Penilaian dan debrief
Debrief memuat: objektif selesai/gagal, event (gangguan, tabrakan, alarm), jejak lintasan, catatan instruktur, lalu dapat diekspor sebagai JSON. Objektif yang melewati batas waktu ditandai gagal dan skenario lanjut ke objektif berikutnya.

## 7. Pemecahan masalah
| Gejala | Penyebab / tindakan |
|---|---|
| ROV tidak bergerak | Jendela game tidak fokus; klik jendela game. Cek profil ROV (hanya Tortuga dan Teledyne aktif). |
| Lampu tidak bisa dinyalakan | Instruktur menyuntik "Lampu padam". |
| Identifikasi tidak maju | Terlalu jauh, kamera tidak mengarah ke objek, atau lampu mati di bawah 40 m. |
| "Target tidak ditemukan" di linimasa | Target belum dibuat; menunggu dasar laut termuat (maks ±8 detik, lalu memakai kedalaman cadangan). |
| Dasar laut kosong/tanpa Cesium | Server tile lokal belum jalan; simulasi tetap berjalan tanpa internet. |
