using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public static class Localization
    {
        private const string DefaultLanguageCode = "en";

        private static readonly LanguageOption[] Options =
        {
            new LanguageOption(string.Empty, "System"),
            new LanguageOption("en", "English"),
            new LanguageOption("ko", "한국어"),
            new LanguageOption("ja", "日本語"),
            new LanguageOption("zh-Hans", "中文简体"),
            new LanguageOption("zh-Hant", "中文繁體"),
            new LanguageOption("th", "ไทย"),
            new LanguageOption("vi", "Tiếng Việt"),
            new LanguageOption("id", "Indonesia"),
            new LanguageOption("es", "Español"),
            new LanguageOption("pt-BR", "Português"),
            new LanguageOption("de", "Deutsch"),
            new LanguageOption("fr", "Français")
        };

        private static readonly string[] Rows =
        {
            "key|en|ko|ja|zh-Hans|zh-Hant|th|vi|id|es|pt-BR|de|fr",
            "clear_title|CLEAR|클리어|クリア|通关|通關|ผ่านด่าน|Hoàn thành|Selesai|COMPLETADO|CONCLUIDO|GESCHAFFT|RÉUSSI",
            "clear_stage|Stage {0:00} Clear|스테이지 {0:00} 클리어|ステージ {0:00} クリア|第 {0:00} 关通关|第 {0:00} 關通關|ด่าน {0:00} ผ่านแล้ว|Màn {0:00} hoàn thành|Stage {0:00} selesai|Nivel {0:00} completado|Fase {0:00} concluída|Level {0:00} geschafft|Niveau {0:00} réussi",
            "reward_gold|+{0} Gold|+{0} 골드|+{0} ゴールド|+{0} 金币|+{0} 金幣|+{0} ทอง|+{0} vàng|+{0} emas|+{0} oro|+{0} ouro|+{0} Gold|+{0} or",
            "reward_claimed|Reward Claimed|보상 수령 완료|報酬受け取り済み|奖励已领取|獎勵已領取|รับรางวัลแล้ว|Đã nhận thưởng|Hadiah diterima|Recompensa reclamada|Recompensa recebida|Belohnung erhalten|Récompense reçue",
            "reward_double_ad|Watch Ad x2|광고 보고 x2|広告でx2|看广告x2|看廣告x2|ดูโฆษณา x2|Xem quảng cáo x2|Tonton iklan x2|Ver anuncio x2|Assistir anúncio x2|Anzeige x2|Pub x2",
            "reward_doubled|x2 Claimed|x2 완료|x2 受け取り済み|x2 已领取|x2 已領取|รับ x2 แล้ว|Đã nhận x2|x2 diterima|x2 recibido|x2 recebido|x2 erhalten|x2 reçu",
            "tutorial_tap_bus|Tap a bus to send it to the stop.|버스를 눌러 정류장으로 보내세요.|バスをタップして停留所へ送ろう。|点击巴士送到站位。|點擊巴士送到站位。|แตะรถบัสเพื่อส่งไปจุดจอด|Chạm xe để đưa vào bến.|Ketuk bus untuk mengirim ke halte.|Toca un bus para enviarlo.|Toque no ônibus para enviar à parada.|Tippe auf einen Bus zur Haltestelle.|Touchez un bus pour l'envoyer.",
            "tutorial_bus_depart|When the bus is full, it leaves automatically.|가득 차면 차량이 자동으로 출발합니다.|満員になると自動で出発します。|坐满后会自动发车。|坐滿後會自動發車。|เมื่อเต็มแล้วรถจะออกเอง|Xe đầy sẽ tự rời bến.|Jika penuh, bus berangkat otomatis.|Cuando se llena, sale solo.|Quando lota, sai sozinho.|Wenn voll, fährt er automatisch ab.|Une fois plein, il part tout seul.",
            "tutorial_finish_all|Send every bus and board all passengers to clear.|모든 승객을 태우면 클리어됩니다.|全員を乗せるとクリアです。|载完所有乘客即可通关。|載完所有乘客即可通關。|รับผู้โดยสารทั้งหมดเพื่อผ่านด่าน|Đón hết khách để qua màn.|Naikkan semua penumpang untuk selesai.|Sube a todos para completar.|Embarque todos para concluir.|Alle einsteigen lassen zum Sieg.|Faites monter tout le monde.",
            "tutorial_fast_forward|Hold an empty area to speed up passenger movement.|빈 곳을 누르고 있으면 승객 이동이 빨라집니다.|空いている場所を長押しで早送り。|长按空白处可加速乘客移动。|長按空白處可加速乘客移動。|กดพื้นที่ว่างค้างเพื่อเร่งผู้โดยสาร|Giữ vùng trống để tăng tốc khách.|Tahan area kosong untuk mempercepat.|Mantén un espacio vacío para acelerar.|Segure vazio para acelerar.|Freie Fläche halten zum Beschleunigen.|Maintenez une zone vide pour accélérer.",
            "tutorial_plus_free|Try + Slot once for free.|+ 정류장 슬롯을 1회 무료로 써보세요.|+スロットを1回無料で試そう。|免费试用一次 + 车位。|免費試用一次 + 車位。|ลอง + ช่องฟรี 1 ครั้ง|Thử + ô miễn phí một lần.|Coba + slot gratis sekali.|Prueba + plaza gratis una vez.|Teste + vaga grátis uma vez.|+ Platz einmal gratis testen.|Essayez + place gratuitement.",
            "tutorial_mix_free|Try Shuffle once for free when buses are mixed up.|막혔을 때 섞기를 1회 무료로 써보세요.|困ったらミックスを1回無料で。|卡住时免费试用一次混合。|卡住時免費試用一次混合。|ลองสลับฟรีเมื่อรถติด|Thử trộn miễn phí khi bị kẹt.|Coba acak gratis saat macet.|Usa mezclar gratis una vez.|Use misturar grátis uma vez.|Mischen einmal gratis testen.|Mélange gratuit une fois.",
            "tutorial_depart_free|Try Depart once for free to fill waiting buses.|정류장 차량 채우기를 1회 무료로 써보세요.|出発補助を1回無料で試そう。|免费试用一次发车辅助。|免費試用一次發車輔助。|ลองส่งรถออกฟรี 1 ครั้ง|Thử rời bến miễn phí một lần.|Coba berangkat gratis sekali.|Prueba salida gratis una vez.|Teste saída grátis uma vez.|Abfahrt einmal gratis testen.|Départ gratuit une fois.",
            "tutorial_vip_hint|VIP moves one waiting bus straight to the special stop.|VIP는 대기 차량 하나를 특별 정류장으로 보냅니다.|VIPは待機中のバスを特別枠へ送ります。|VIP 可把等待巴士送到特殊站位。|VIP 可把等待巴士送到特殊站位。|VIP ส่งรถหนึ่งคันไปจุดพิเศษ|VIP đưa một xe vào bến đặc biệt.|VIP memindah satu bus ke halte khusus.|VIP mueve un bus a la parada especial.|VIP leva um ônibus à parada especial.|VIP schickt einen Bus zum Sonderplatz.|VIP envoie un bus à l'arrêt spécial.",
            "tutorial_plus_unlocked|A locked stop has opened.|잠긴 정류장 칸이 열렸습니다.|ロックされた停留所が開きました。|已解锁一个站位。|已解鎖一個站位。|ปลดล็อกจุดจอดแล้ว|Đã mở một ô bến.|Satu halte terbuka.|Se abrió una parada.|Uma parada foi aberta.|Ein Platz wurde geöffnet.|Un arrêt est ouvert.",
            "tutorial_mix_done|Bus colors have been shuffled.|차량 색상이 섞였습니다.|バスの色を混ぜました。|巴士颜色已混合。|巴士顏色已混合。|สลับสีรถแล้ว|Đã trộn màu xe.|Warna bus diacak.|Colores mezclados.|Cores misturadas.|Farben gemischt.|Couleurs mélangées.",
            "tutorial_depart_done|Waiting buses are being filled.|정류장 차량을 채우고 있습니다.|停留所のバスを埋めています。|正在填满等待巴士。|正在填滿等待巴士。|กำลังเติมรถที่รออยู่|Đang lấp đầy xe chờ.|Bus tunggu sedang diisi.|Llenando buses en espera.|Preenchendo ônibus na parada.|Wartende Busse werden gefüllt.|Les bus en attente se remplissent.",
            "next|Next|다음|次へ|下一关|下一關|ถัดไป|Tiếp|Berikutnya|Siguiente|Próximo|Weiter|Suivant",
            "done|Done|완료|完了|完成|完成|เสร็จสิ้น|Xong|Selesai|Listo|Concluído|Fertig|Terminé",
            "failed_title|FAILED|실패|失敗|失败|失敗|ล้มเหลว|Thất bại|Gagal|FALLASTE|FALHOU|FEHLGESCHLAGEN|ÉCHEC",
            "stage_failed|Stage Failed|스테이지 실패|ステージ失敗|关卡失败|關卡失敗|ด่านล้มเหลว|Màn thất bại|Stage gagal|Nivel fallido|Fase falhou|Level fehlgeschlagen|Niveau échoué",
            "recover_or_retry|Recover or Retry|회복 또는 재시도|回復またはリトライ|恢复或重试|恢復或重試|กู้คืนหรือเล่นใหม่|Cứu vãn hoặc thử lại|Pulihkan atau coba lagi|Recuperar o reintentar|Recuperar ou tentar de novo|Retten oder neu versuchen|Récupérer ou réessayer",
            "retry_stage|Retry Stage|스테이지 재시도|ステージをリトライ|重试关卡|重試關卡|เล่นด่านใหม่|Thử lại màn|Coba stage lagi|Reintentar nivel|Tentar fase de novo|Level neu versuchen|Réessayer le niveau",
            "retry|Retry|재시도|リトライ|重试|重試|ลองใหม่|Thử lại|Coba lagi|Reintentar|Tentar de novo|Neu versuchen|Réessayer",
            "plus_slot|+ Slot|+ 슬롯|+ スロット|+ 车位|+ 車位|+ ช่อง|+ Ô đỗ|+ Slot|+ Plaza|+ Vaga|+ Platz|+ Place",
            "locked|Locked|잠김|ロック中|已锁定|已鎖定|ล็อกอยู่|Đã khóa|Terkunci|Bloqueado|Bloqueado|Gesperrt|Verrouillé",
            "exit_title|EXIT|나가기|終了|退出|退出|ออก|Thoát|Keluar|SALIR|SAIR|BEENDEN|QUITTER",
            "exit_game|Exit Game?|게임을 종료할까요?|ゲームを終了しますか？|退出游戏？|退出遊戲？|ออกจากเกมไหม?|Thoát trò chơi?|Keluar dari game?|¿Salir del juego?|Sair do jogo?|Spiel beenden?|Quitter le jeu ?",
            "exit|Exit|나가기|終了|退出|退出|ออก|Thoát|Keluar|Salir|Sair|Beenden|Quitter",
            "slot_title|SLOT|슬롯|スロット|车位|車位|ช่อง|Ô đỗ|Slot|Plaza|Vaga|Platz|Place",
            "watch|Watch|광고 보기|視聴|观看|觀看|ดูโฆษณา|Xem|Tonton|Ver|Assistir|Ansehen|Regarder",
            "loading|Loading|로딩 중|読み込み中|加载中|載入中|กำลังโหลด|Đang tải|Memuat|Cargando|Carregando|Lädt|Chargement",
            "loading_ad|Loading Ad|광고 로딩 중|広告を読み込み中|广告加载中|廣告載入中|กำลังโหลดโฆษณา|Đang tải quảng cáo|Memuat iklan|Cargando anuncio|Carregando anúncio|Anzeige lädt|Chargement de la pub",
            "ad_unavailable|Ad Unavailable|광고 없음|広告なし|暂无广告|暫無廣告|ไม่มีโฆษณา|Không có quảng cáo|Iklan tidak tersedia|Anuncio no disponible|Anúncio indisponível|Keine Anzeige|Pub indisponible",
            "ad_unavailable_try_later|Ad unavailable. Try again later.|광고를 불러올 수 없습니다. 잠시 후 다시 시도하세요.|広告を読み込めません。後でもう一度お試しください。|广告暂不可用。请稍后再试。|廣告暫不可用。請稍後再試。|ไม่มีโฆษณา ลองใหม่ภายหลัง|Không có quảng cáo. Hãy thử lại sau.|Iklan belum tersedia. Coba lagi nanti.|Anuncio no disponible. Intenta más tarde.|Anúncio indisponível. Tente mais tarde.|Keine Anzeige verfügbar. Später erneut versuchen.|Pub indisponible. Réessayez plus tard.",
            "watch_ad_stop|Watch Ad?\\n+1 Stop ({0})|광고를 볼까요?\\n+1 정류장 ({0})|広告を見ますか？\\n+1 停留所 ({0})|观看广告？\\n+1 站位 ({0})|觀看廣告？\\n+1 站位 ({0})|ดูโฆษณาไหม?\\n+1 จุดจอด ({0})|Xem quảng cáo?\\n+1 điểm dừng ({0})|Tonton iklan?\\n+1 halte ({0})|¿Ver anuncio?\\n+1 parada ({0})|Assistir anúncio?\\n+1 parada ({0})|Anzeige ansehen?\\n+1 Halteplatz ({0})|Regarder une pub ?\\n+1 arrêt ({0})",
            "vip_bus_gold_or_ad|VIP Bus ({0})\\nUse Gold or Watch Ad|VIP 버스 ({0})\\n골드 사용 또는 광고 보기|VIPバス ({0})\\nゴールド使用または広告視聴|VIP巴士 ({0})\\n使用金币或观看广告|VIP巴士 ({0})\\n使用金幣或觀看廣告|รถ VIP ({0})\\nใช้ทองหรือดูโฆษณา|Xe VIP ({0})\\nDùng vàng hoặc xem quảng cáo|Bus VIP ({0})\\nPakai emas atau tonton iklan|Bus VIP ({0})\\nUsa oro o ve anuncio|Ônibus VIP ({0})\\nUse ouro ou assista anúncio|VIP-Bus ({0})\\nGold nutzen oder Anzeige ansehen|Bus VIP ({0})\\nUtiliser de l'or ou regarder une pub",
            "vip_bus_gold_balance|VIP Bus ({0})\\nGold {1}/{2}|VIP 버스 ({0})\\n골드 {1}/{2}|VIPバス ({0})\\nゴールド {1}/{2}|VIP巴士 ({0})\\n金币 {1}/{2}|VIP巴士 ({0})\\n金幣 {1}/{2}|รถ VIP ({0})\\nทอง {1}/{2}|Xe VIP ({0})\\nVàng {1}/{2}|Bus VIP ({0})\\nEmas {1}/{2}|Bus VIP ({0})\\nOro {1}/{2}|Ônibus VIP ({0})\\nOuro {1}/{2}|VIP-Bus ({0})\\nGold {1}/{2}|Bus VIP ({0})\\nOr {1}/{2}",
            "cost_gold|{0} Gold|{0} 골드|{0} ゴールド|{0} 金币|{0} 金幣|{0} ทอง|{0} vàng|{0} emas|{0} oro|{0} ouro|{0} Gold|{0} or",
            "need_gold|Need Gold|골드 부족|ゴールド不足|金币不足|金幣不足|ทองไม่พอ|Thiếu vàng|Emas kurang|Falta oro|Falta ouro|Zu wenig Gold|Pas assez d'or",
            "mix_buses_gold_or_ad|Mix Buses\\nUse Gold or Watch Ad|버스 섞기\\n골드 사용 또는 광고 보기|バスをミックス\\nゴールド使用または広告視聴|混合巴士\\n使用金币或观看广告|混合巴士\\n使用金幣或觀看廣告|สลับรถบัส\\nใช้ทองหรือดูโฆษณา|Trộn xe\\nDùng vàng hoặc xem quảng cáo|Acak bus\\nPakai emas atau tonton iklan|Mezclar buses\\nUsa oro o ve anuncio|Misturar ônibus\\nUse ouro ou assista anúncio|Busse mischen\\nGold nutzen oder Anzeige ansehen|Mélanger les bus\\nUtiliser de l'or ou regarder une pub",
            "mix_buses_gold_balance|Mix Buses\\nGold {0}/{1}|버스 섞기\\n골드 {0}/{1}|バスをミックス\\nゴールド {0}/{1}|混合巴士\\n金币 {0}/{1}|混合巴士\\n金幣 {0}/{1}|สลับรถบัส\\nทอง {0}/{1}|Trộn xe\\nVàng {0}/{1}|Acak bus\\nEmas {0}/{1}|Mezclar buses\\nOro {0}/{1}|Misturar ônibus\\nOuro {0}/{1}|Busse mischen\\nGold {0}/{1}|Mélanger les bus\\nOr {0}/{1}",
            "depart_buses_gold_or_ad|Depart Buses\\nUse Gold or Watch Ad|버스 출발\\n골드 사용 또는 광고 보기|バスを出発\\nゴールド使用または広告視聴|发车\\n使用金币或观看广告|發車\\n使用金幣或觀看廣告|ส่งรถออก\\nใช้ทองหรือดูโฆษณา|Cho xe rời bến\\nDùng vàng hoặc xem quảng cáo|Berangkatkan bus\\nPakai emas atau tonton iklan|Enviar buses\\nUsa oro o ve anuncio|Enviar ônibus\\nUse ouro ou assista anúncio|Busse abfahren lassen\\nGold nutzen oder Anzeige ansehen|Faire partir les bus\\nUtiliser de l'or ou regarder une pub",
            "depart_buses_gold_balance|Depart Buses\\nGold {0}/{1}|버스 출발\\n골드 {0}/{1}|バスを出発\\nゴールド {0}/{1}|发车\\n金币 {0}/{1}|發車\\n金幣 {0}/{1}|ส่งรถออก\\nทอง {0}/{1}|Cho xe rời bến\\nVàng {0}/{1}|Berangkatkan bus\\nEmas {0}/{1}|Enviar buses\\nOro {0}/{1}|Enviar ônibus\\nOuro {0}/{1}|Busse abfahren lassen\\nGold {0}/{1}|Faire partir les bus\\nOr {0}/{1}",
            "vip_title|VIP|VIP|VIP|VIP|VIP|VIP|VIP|VIP|VIP|VIP|VIP|VIP",
            "mix_title|MIX|섞기|ミックス|混合|混合|สลับ|Trộn|Acak|Mezclar|Misturar|Mischen|Mélanger",
            "depart|Depart|출발|出発|发车|發車|ออก|Rời bến|Berangkat|Salir|Sair|Abfahren|Départ",
            "update|Update|업데이트|更新|更新|更新|อัปเดต|Cập nhật|Perbarui|Actualizar|Atualizar|Aktualisieren|Mettre à jour",
            "update_required|Update Required|업데이트 필요|更新が必要|需要更新|需要更新|ต้องอัปเดต|Cần cập nhật|Perlu diperbarui|Actualización requerida|Atualização necessária|Update erforderlich|Mise à jour requise",
            "maintenance_title|Maintenance|점검 중|メンテナンス中|维护中|維護中|ปิดปรับปรุง|Bảo trì|Pemeliharaan|Mantenimiento|Manutenção|Wartung|Maintenance",
            "cancel|Cancel|취소|キャンセル|取消|取消|ยกเลิก|Hủy|Batal|Cancelar|Cancelar|Abbrechen|Annuler",
            "pick|Pick|선택|選択|选择|選擇|เลือก|Chọn|Pilih|Elegir|Escolher|Auswählen|Choisir",
            "status_clear|Clear|클리어|クリア|通关|通關|ผ่าน|Hoàn thành|Selesai|Completado|Concluído|Geschafft|Réussi",
            "status_all_clear|All Clear|전체 클리어|全クリア|全部通关|全部通關|ผ่านทั้งหมด|Hoàn thành tất cả|Semua selesai|Todo completado|Tudo concluído|Alles geschafft|Tout réussi",
            "status_failed|Failed|실패|失敗|失败|失敗|ล้มเหลว|Thất bại|Gagal|Fallido|Falhou|Fehlgeschlagen|Échec",
            "settings_title|OPTION|옵션|オプション|选项|選項|ตัวเลือก|Tùy chọn|Opsi|OPCIONES|OPÇÕES|OPTIONEN|OPTIONS",
            "effect_sound|Effect|효과음|効果音|音效|音效|เสียงเอฟเฟกต์|Hiệu ứng|Efek|Efectos|Efeitos|Effekte|Effets",
            "music|Music|음악|音楽|音乐|音樂|เพลง|Nhạc|Musik|Música|Música|Musik|Musique",
            "vibration|Vibration|진동|振動|振动|震動|สั่น|Rung|Getar|Vibración|Vibração|Vibration|Vibration",
            "language|Language|언어|言語|语言|語言|ภาษา|Ngôn ngữ|Bahasa|Idioma|Idioma|Sprache|Langue",
            "language_title|LANGUAGE|언어|言語|语言|語言|ภาษา|Ngôn ngữ|BAHASA|IDIOMA|IDIOMA|SPRACHE|LANGUE",
            "language_system|System|기기 설정|システム|系统|系統|ระบบ|Hệ thống|Sistem|Sistema|Sistema|System|Système",
            "contact_short|Contact|문의|お問い合わせ|联系|聯絡|ติดต่อ|Liên hệ|Kontak|Contacto|Contato|Kontakt|Contact",
            "legal_short|Policy|약관|規約|政策|政策|นโยบาย|Chính sách|Kebijakan|Política|Política|Richtlinie|Politique",
            "feedback_subject|Bus Pop Feedback|Bus Pop 피드백|Bus Pop フィードバック|Bus Pop 反馈|Bus Pop 回饋|ข้อเสนอแนะ Bus Pop|Góp ý Bus Pop|Masukan Bus Pop|Comentarios de Bus Pop|Feedback do Bus Pop|Bus Pop Feedback|Retour Bus Pop",
            "feedback_body|Please write your feedback here.|피드백을 여기에 작성해 주세요.|ここにフィードバックを書いてください。|请在这里写下你的反馈。|請在這裡寫下你的回饋。|โปรดเขียนข้อเสนอแนะที่นี่|Vui lòng viết góp ý tại đây.|Silakan tulis masukan Anda di sini.|Escribe tus comentarios aquí.|Escreva seu feedback aqui.|Bitte schreibe hier dein Feedback.|Écrivez votre retour ici.",
            "status_mystery_bus|Mystery bus|숨겨진 버스|謎のバス|神秘巴士|神秘巴士|รถบัสปริศนา|Xe bí ẩn|Bus misteri|Bus misterioso|Ônibus misterioso|Mystery-Bus|Bus mystère",
            "status_station_full|Station full|정류장이 가득 찼어요|停留所が満車です|站位已满|站位已滿|จุดจอดเต็ม|Bến đã đầy|Halte penuh|Parada llena|Parada cheia|Halteplatz voll|Arrêt plein",
            "status_blocked|Blocked|막혔어요|ブロック中|被挡住了|被擋住了|ถูกขวาง|Bị chặn|Terhalang|Bloqueado|Bloqueado|Blockiert|Bloqué",
            "status_bus_dispatched|{0} bus dispatched|{0} 버스 출발|{0}バス出発|{0}巴士已出发|{0}巴士已出發|รถบัส{0}ออกแล้ว|Xe {0} đã đi|Bus {0} berangkat|Bus {0} enviado|Ônibus {0} saiu|Bus ({0}) fährt los|Bus {0} parti",
            "status_vip_busy|VIP busy|VIP 정류장 사용 중|VIP使用中|VIP正忙|VIP使用中|VIP ไม่ว่าง|VIP đang bận|VIP sibuk|VIP ocupado|VIP ocupado|VIP belegt|VIP occupé",
            "status_bus_vip|{0} VIP|{0} VIP|{0} VIP|{0} VIP|{0} VIP|VIP {0}|{0} VIP|{0} VIP|{0} VIP|{0} VIP|{0} VIP|{0} VIP",
            "status_no_vip_target|No VIP target|VIP 대상 없음|VIP対象なし|没有VIP目标|沒有VIP目標|ไม่มีเป้าหมาย VIP|Không có xe VIP|Tidak ada target VIP|Sin objetivo VIP|Sem alvo VIP|Kein VIP-Ziel|Aucune cible VIP",
            "status_choose_vip_bus|Choose VIP bus|VIP 버스를 선택하세요|VIPバスを選択|选择VIP巴士|選擇VIP巴士|เลือกรถ VIP|Chọn xe VIP|Pilih bus VIP|Elige bus VIP|Escolha o ônibus VIP|VIP-Bus wählen|Choisir un bus VIP",
            "status_pick_waiting_bus|Pick waiting bus|대기 중인 버스를 선택하세요|待機中のバスを選択|选择等待中的巴士|選擇等待中的巴士|เลือกรถที่รออยู่|Chọn xe đang chờ|Pilih bus yang menunggu|Elige un bus en espera|Escolha um ônibus aguardando|Wartenden Bus wählen|Choisir un bus en attente",
            "status_no_mix_target|No mix target|섞을 대상 없음|ミックス対象なし|没有可混合目标|沒有可混合目標|ไม่มีเป้าหมายให้สลับ|Không có xe để trộn|Tidak ada target acak|Sin objetivo para mezclar|Sem alvo para misturar|Kein Mischziel|Aucune cible à mélanger",
            "status_mixed|Mixed|섞기 완료|ミックス完了|已混合|已混合|สลับแล้ว|Đã trộn|Sudah diacak|Mezclado|Misturado|Gemischt|Mélangé",
            "status_no_depart_target|No depart target|출발 대상 없음|出発対象なし|没有可发车目标|沒有可發車目標|ไม่มีรถให้ออก|Không có xe để rời bến|Tidak ada target berangkat|Sin objetivo para salir|Sem alvo para sair|Kein Abfahrtsziel|Aucune cible de départ",
            "status_departing|Departing|출발 중|出発中|发车中|發車中|กำลังออก|Đang rời bến|Berangkat|Saliendo|Saindo|Fährt ab|Départ en cours",
            "units_count|Units {0}|유닛 {0}|ユニット {0}|乘客 {0}|乘客 {0}|ยูนิต {0}|Đơn vị {0}|Unit {0}|Unidades {0}|Unidades {0}|Einheiten {0}|Unités {0}",
            "stops_count|Stops {0}/{1}|정류장 {0}/{1}|停留所 {0}/{1}|站位 {0}/{1}|站位 {0}/{1}|จุดจอด {0}/{1}|Điểm dừng {0}/{1}|Halte {0}/{1}|Paradas {0}/{1}|Paradas {0}/{1}|Halte {0}/{1}|Arrêts {0}/{1}",
            "color_red|Red|빨간색|赤|红色|紅色|แดง|đỏ|merah|rojo|vermelho|rot|rouge",
            "color_blue|Blue|파란색|青|蓝色|藍色|น้ำเงิน|xanh dương|biru|azul|azul|blau|bleu",
            "color_yellow|Yellow|노란색|黄|黄色|黃色|เหลือง|vàng|kuning|amarillo|amarelo|gelb|jaune",
            "color_green|Green|초록색|緑|绿色|綠色|เขียว|xanh lá|hijau|verde|verde|grün|vert",
            "color_purple|Purple|보라색|紫|紫色|紫色|ม่วง|tím|ungu|morado|roxo|lila|violet",
            "color_orange|Orange|주황색|オレンジ|橙色|橘色|ส้ม|cam|oranye|naranja|laranja|orange|orange",
            "color_white|White|흰색|白|白色|白色|ขาว|trắng|putih|blanco|branco|weiß|blanc",
            "color_black|Black|검은색|黒|黑色|黑色|ดำ|đen|hitam|negro|preto|schwarz|noir",
            "color_pink|Pink|분홍색|ピンク|粉色|粉紅色|ชมพู|hồng|merah muda|rosa|rosa|pink|rose",
            "color_sky_blue|Sky Blue|하늘색|水色|天蓝色|天藍色|ฟ้า|xanh da trời|biru langit|celeste|azul claro|hellblau|bleu ciel",
            "color_unknown|Unknown|알 수 없음|不明|未知|未知|ไม่ทราบ|không rõ|tidak diketahui|desconocido|desconhecido|unbekannt|inconnu"
        };

        private static Dictionary<string, Dictionary<string, string>> tables;
        private static string[] languageCodes;
        private static string cachedLanguageCode;

        public readonly struct LanguageOption
        {
            public readonly string Code;
            public readonly string NativeName;

            public LanguageOption(string code, string nativeName)
            {
                Code = code ?? string.Empty;
                NativeName = nativeName ?? string.Empty;
            }

            public bool IsSystem => string.IsNullOrEmpty(Code);
        }

        public static IReadOnlyList<LanguageOption> LanguageOptions => Options;

        public static string SelectedLanguageCode
        {
            get
            {
                var languageCode = UserPreferences.LanguageCode;
                return IsSupportedLanguageCode(languageCode) ? languageCode : string.Empty;
            }
            set
            {
                UserPreferences.LanguageCode = IsSupportedLanguageCode(value) ? value : string.Empty;
                cachedLanguageCode = null;
            }
        }

        public static bool IsSystemLanguageSelected => string.IsNullOrEmpty(SelectedLanguageCode);

        public static string CurrentLanguageCode
        {
            get
            {
                if (string.IsNullOrEmpty(cachedLanguageCode))
                {
                    var selectedLanguageCode = UserPreferences.LanguageCode;
                    cachedLanguageCode = IsSupportedLanguageCode(selectedLanguageCode)
                        ? selectedLanguageCode
                        : NormalizeLanguage(Application.systemLanguage);
                }

                return cachedLanguageCode;
            }
        }

        public static string Text(string key, params object[] args)
        {
            EnsureTables();
            var value = GetRawText(CurrentLanguageCode, key);
            if (args == null || args.Length == 0)
            {
                return value;
            }

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        public static string GetLanguageOptionLabel(LanguageOption option)
        {
            return option.IsSystem ? Text("language_system") : option.NativeName;
        }

        public static string ColorName(PuzzleColor color)
        {
            switch (color)
            {
                case PuzzleColor.Red:
                    return Text("color_red");
                case PuzzleColor.Blue:
                    return Text("color_blue");
                case PuzzleColor.Yellow:
                    return Text("color_yellow");
                case PuzzleColor.Green:
                    return Text("color_green");
                case PuzzleColor.Purple:
                    return Text("color_purple");
                case PuzzleColor.Orange:
                    return Text("color_orange");
                case PuzzleColor.White:
                    return Text("color_white");
                case PuzzleColor.Black:
                    return Text("color_black");
                case PuzzleColor.Pink:
                    return Text("color_pink");
                case PuzzleColor.SkyBlue:
                    return Text("color_sky_blue");
                default:
                    return Text("color_unknown");
            }
        }

        private static string GetRawText(string languageCode, string key)
        {
            if (tables.TryGetValue(languageCode, out var table) &&
                table.TryGetValue(key, out var value))
            {
                return value;
            }

            if (tables.TryGetValue(DefaultLanguageCode, out var defaultTable) &&
                defaultTable.TryGetValue(key, out var defaultValue))
            {
                return defaultValue;
            }

            return key;
        }

        private static bool IsSupportedLanguageCode(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return true;
            }

            for (var index = 0; index < Options.Length; index++)
            {
                if (Options[index].Code == languageCode)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureTables()
        {
            if (tables != null)
            {
                return;
            }

            var header = Rows[0].Split('|');
            languageCodes = new string[header.Length - 1];
            tables = new Dictionary<string, Dictionary<string, string>>();
            for (var index = 1; index < header.Length; index++)
            {
                var code = header[index];
                languageCodes[index - 1] = code;
                tables[code] = new Dictionary<string, string>();
            }

            for (var rowIndex = 1; rowIndex < Rows.Length; rowIndex++)
            {
                var cells = Rows[rowIndex].Split('|');
                if (cells.Length == 0)
                {
                    continue;
                }

                var key = cells[0];
                for (var columnIndex = 1; columnIndex < cells.Length && columnIndex <= languageCodes.Length; columnIndex++)
                {
                    tables[languageCodes[columnIndex - 1]][key] = cells[columnIndex].Replace("\\n", "\n");
                }
            }
        }

        private static string NormalizeLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Korean:
                    return "ko";
                case SystemLanguage.Japanese:
                    return "ja";
                case SystemLanguage.ChineseSimplified:
                    return "zh-Hans";
                case SystemLanguage.ChineseTraditional:
                    return "zh-Hant";
                case SystemLanguage.Chinese:
                    return "zh-Hans";
                case SystemLanguage.Thai:
                    return "th";
                case SystemLanguage.Vietnamese:
                    return "vi";
                case SystemLanguage.Indonesian:
                    return "id";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Portuguese:
                    return "pt-BR";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.French:
                    return "fr";
                default:
                    return DefaultLanguageCode;
            }
        }
    }
}
