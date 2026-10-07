# ROV Trainer Simulator (Unity) — Pushidrosal

Prototype simulator pelatihan operator ROV untuk Pushidrosal TNI AL. Dibangun oleh Software Development Division, Falah Inovasi Teknologi.
Satu platform Unity, empat profil ROV sebagai data (ScriptableObject). Prototype ini = Tortuga + Teledyne (ROV kecil) saja.

## Baca dulu sebelum mengerjakan task apa pun
1. `docs/SPEC.md` — apa yang dibangun, scope prototype, apa yang TIDAK dibangun
2. `docs/ARCHITECTURE.md` — struktur proyek, asmdef, model fisika, profil ROV
3. `docs/BACKLOG.md` — skema pengerjaan untuk proyek yang SUDAH ADA; kerjakan SATU task per sesi, urut
4. `docs/STATE.md` — kondisi terkini proyek (hasil audit, keputusan, task selesai). Perbarui di akhir setiap sesi
5. `docs/MODEL_INTEGRATION.md` — aturan memasukkan model ROV ke simulator
(`docs/BACKLOG_ORIGINAL.md` hanya rujukan: prompt lengkap task T1.x–T6.x ada di sana)

## Aturan kerja
- Kerjakan satu task BACKLOG per sesi. Mulai dengan menulis rencana singkat, tunggu persetujuan bila task menyentuh banyak file.
- Jangan mengerjakan task di luar yang diminta. Jangan refactor file yang tidak terkait.
- Setiap task selesai harus: compile tanpa error, test yang relevan hijau, dan lolos "Kriteria selesai" task tersebut.
- Task berlabel **MANUAL** adalah pekerjaan manusia (Inspector, layout visual, tuning angka, aset 3D). Jangan dikerjakan lewat skrip; tulis daftar langkahnya saja.
- Jangan mengarang data teknis ROV. Parameter yang belum dikonfirmasi ditulis `[___]` di profil, dengan nilai sementara yang diberi komentar `// PLACEHOLDER`.
- Jangan commit tanpa diminta. Pesan commit bahasa Inggris, imperatif, satu baris ringkas.

## Konvensi kode
- Unity: versi di `ProjectSettings/ProjectVersion.txt`; jangan upgrade versi tanpa persetujuan.
- C#: namespace `Falah.RovSim.<Modul>`, satu kelas publik per file, nama file = nama kelas.
- Satu asmdef per modul (lihat ARCHITECTURE). Dependensi hanya ke arah bawah; `Core` tidak boleh mereferensi modul lain.
- Fisika di `FixedUpdate`, input dibaca di `Update` lalu disimpan, tampilan di `Update/LateUpdate`. Tidak ada alokasi per frame di jalur fisika dan sensor.
- Parameter ROV dibaca dari `RovProfile`, tidak boleh ditulis keras di skrip. Angka ajaib dijadikan `[SerializeField]` atau field profil.
- Sistem input: Input System package (bukan `UnityEngine.Input`).
- Logika yang bisa diuji (pengalokasi thrust, PID, drag, perekam) ditulis sebagai kelas C# biasa tanpa MonoBehaviour, dan diuji dengan EditMode test.
- Komentar dan nama identifier bahasa Inggris. Teks UI lewat tabel string (siap lokalisasi), default bahasa Indonesia.

## Lingkungan
- Windows, proyek di `D:\`. Unity MCP (CoplayDev) port 49160 dipakai untuk aksi editor.
- Tool MCP berisiko tinggi (`Execute Code`, `Execute Menu Item`, `Batch Execute`) tidak boleh auto-allow; minta persetujuan tiap kali.
- Pembagian peran: Claude Code mengerjakan logika, skrip, dan tooling editor. Manusia mengerjakan layout visual, assign Inspector, tuning nilai, dan aset.
- Tangkapan layar di HDRP: gunakan `ScreenCapture.CaptureScreenshot()` ke disk, bukan `CaptureScreenshotAsTexture()`.

## Perintah umum
- Jalankan test EditMode/PlayMode lewat Unity Test Runner (atau `unity-cli` bila terpasang). Catat perintah yang berhasil di bagian ini setelah T0.1.

## Proyek ini SUDAH ADA (penting)
Proyek Unity sudah berisi: Cesium for Unity, air, dan model objek. Itu aset kerja yang harus dipakai, bukan dibuat ulang.
- Jangan memindahkan, mengganti nama, menghapus, atau mengatur ulang aset, scene, dan folder yang sudah ada tanpa persetujuan eksplisit. Kode baru masuk ke `Assets/_Falah/` (sesuai ARCHITECTURE bagian 2).
- Jangan upgrade Unity, Cesium, atau render pipeline. Pipeline yang terpasang adalah yang dipakai; catat di STATE.md.
- Jangan mengubah setelan Cesium (georeference, tileset, URL) atau material air yang ada tanpa mencatat nilai lama di STATE.md lebih dulu.
- Kerjakan di branch `rov-sim/<id-task>`. Sebelum mengubah apa pun, pastikan working tree bersih atau sudah di-commit manusia.
- Task pertama selalu A0.1 (audit, hanya membaca). Jangan mulai task lain sebelum `docs/AUDIT.md` dan `docs/STATE.md` terisi.
- Lingkungan di bagian "Lingkungan" di atas adalah asumsi; bila audit menunjukkan berbeda (mis. URP, bukan HDRP), ikuti hasil audit dan perbaiki bagian ini.
