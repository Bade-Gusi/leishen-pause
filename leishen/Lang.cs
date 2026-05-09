namespace leishen
{
    public static class Lang
    {
        public static string CurrentLang = "zh";

        // 所有支持的语言列表 (语言代码 -> 显示名称)
        public static Dictionary<string, string> Languages => new()
        {
            ["zh"] = "中文",        // 中文简体
            ["en"] = "English",     // 英语
            ["ja"] = "日本語",      // 日语
            ["ko"] = "한국어",      // 韩语
            ["es"] = "Español",     // 西班牙语
            ["fr"] = "Français",    // 法语
            ["de"] = "Deutsch",     // 德语
            ["pt"] = "Português",   // 葡萄牙语
            ["ru"] = "Русский",     // 俄语
            ["ar"] = "العربية",     // 阿拉伯语
            ["hi"] = "हिन्दी",       // 印地语
            ["it"] = "Italiano",    // 意大利语
            ["nl"] = "Nederlands",  // 荷兰语
            ["pl"] = "Polski",      // 波兰语
            ["sv"] = "Svenska",     // 瑞典语
            ["tr"] = "Türkçe",      // 土耳其语
            ["th"] = "ไทย",         // 泰语
            ["vi"] = "Tiếng Việt",  // 越南语
            ["id"] = "Bahasa Indonesia", // 印尼语
            ["ms"] = "Bahasa Melayu",    // 马来语
            ["tl"] = "Filipino",    // 菲律宾语
            ["cs"] = "Čeština",     // 捷克语
            ["hu"] = "Magyar",      // 匈牙利语
            ["ro"] = "Română",      // 罗马尼亚语
            ["bn"] = "বাংলা",       // 孟加拉语
            ["uk"] = "Українська",  // 乌克兰语
            ["el"] = "Ελληνικά",    // 希腊语
            ["da"] = "Dansk",       // 丹麦语
            ["fi"] = "Suomi",       // 芬兰语
            ["no"] = "Norsk",       // 挪威语
            ["sk"] = "Slovenčina",  // 斯洛伐克语
            ["he"] = "עברית",       // 希伯来语
        };

        public static string Get(string key)
        {
            if (CurrentLang != "zh" && Translations.TryGetValue(key, out var langDict)
                && langDict.TryGetValue(CurrentLang, out var val) && !string.IsNullOrEmpty(val))
                return val;
            // fallback to 中文
            if (Zh.TryGetValue(key, out var zhVal))
                return zhVal;
            // final fallback to 英文
            if (En.TryGetValue(key, out var enVal))
                return enVal;
            return key;
        }

        // 翻译表: key -> langCode -> translation
        // 只存储非中文的翻译, 中文用 Zh 字典
        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["app_title"] = new()
            {
                ["en"] = "PUBG Monitor",
                ["ja"] = "PUBGモニター",
                ["ko"] = "PUBG 모니터",
                ["es"] = "Monitor PUBG",
                ["fr"] = "Moniteur PUBG",
                ["de"] = "PUBG-Überwachung",
                ["pt"] = "Monitor PUBG",
                ["ru"] = "Монитор PUBG",
                ["ar"] = "مراقب PUBG",
                ["hi"] = "PUBG मॉनिटर",
                ["it"] = "Monitor PUBG",
                ["nl"] = "PUBG Monitor",
                ["pl"] = "Monitor PUBG",
                ["sv"] = "PUBG-övervakning",
                ["tr"] = "PUBG İzleyici",
                ["th"] = "จอภาพ PUBG",
                ["vi"] = "Giám sát PUBG",
                ["id"] = "Monitor PUBG",
                ["ms"] = "Pemantau PUBG",
                ["tl"] = "PUBG Monitor",
                ["cs"] = "Monitor PUBG",
                ["hu"] = "PUBG Figyelő",
                ["ro"] = "Monitor PUBG",
                ["bn"] = "PUBG মনিটর",
                ["uk"] = "Монітор PUBG",
                ["el"] = "Οθόνη PUBG",
                ["da"] = "PUBG-overvågning",
                ["fi"] = "PUBG-valvonta",
                ["no"] = "PUBG-overvåking",
                ["sk"] = "Monitor PUBG",
                ["he"] = "צג PUBG",
            },
            ["app_subtitle"] = new()
            {
                ["en"] = "Smart Pause Tool",
                ["ja"] = "スマート一時停止ツール",
                ["ko"] = "스마트 일시정지 도구",
                ["es"] = "Herramienta de pausa inteligente",
                ["fr"] = "Outil de pause intelligent",
                ["de"] = "Intelligentes Pausen-Tool",
                ["pt"] = "Ferramenta de pausa inteligente",
                ["ru"] = "Инструмент умной паузы",
                ["th"] = "เครื่องมือหยุดชั่วคราวอัจฉริยะ",
                ["vi"] = "Công cụ tạm dừng thông minh",
                ["id"] = "Alat Jeda Cerdas",
            },
            ["status_label"] = new()
            {
                ["en"] = "Status", ["ja"] = "状態", ["ko"] = "상태",
                ["es"] = "Estado", ["fr"] = "Statut", ["de"] = "Status",
                ["pt"] = "Status", ["ru"] = "Статус", ["it"] = "Stato",
                ["nl"] = "Status", ["th"] = "สถานะ", ["vi"] = "Trạng thái",
            },
            ["status_idle"] = new()
            {
                ["en"] = "Idle", ["ja"] = "待機中", ["ko"] = "대기 중",
                ["es"] = "Inactivo", ["fr"] = "Inactif", ["de"] = "Bereit",
                ["pt"] = "Inativo", ["ru"] = "Ожидание", ["th"] = "ไม่ได้ทำงาน",
                ["vi"] = "Chưa chạy", ["id"] = "Menganggur",
            },
            ["status_scanning"] = new()
            {
                ["en"] = "Scanning", ["ja"] = "スキャン中", ["ko"] = "스캔 중",
                ["es"] = "Escanear", ["fr"] = "Analyse", ["de"] = "Scannen",
                ["pt"] = "Escaneando", ["ru"] = "Сканирование", ["th"] = "กำลังสแกน",
                ["vi"] = "Đang quét", ["id"] = "Memindai",
            },
            ["status_gaming"] = new()
            {
                ["en"] = "In Game", ["ja"] = "プレイ中", ["ko"] = "게임 중",
                ["es"] = "Jugando", ["fr"] = "En jeu", ["de"] = "Im Spiel",
                ["pt"] = "Jogando", ["ru"] = "В игре", ["th"] = "กำลังเล่น",
                ["vi"] = "Đang chơi", ["id"] = "Sedang bermain",
            },
            ["status_paused"] = new()
            {
                ["en"] = "Paused", ["ja"] = "一時停止済", ["ko"] = "일시정지됨",
                ["es"] = "En pausa", ["fr"] = "En pause", ["de"] = "Pausiert",
                ["pt"] = "Pausado", ["ru"] = "На паузе", ["th"] = "หยุดชั่วคราว",
                ["vi"] = "Đã tạm dừng", ["id"] = "Dijeda",
            },
            ["status_waiting"] = new()
            {
                ["en"] = "Waiting for PUBG...",
                ["ja"] = "PUBGを待っています...",
                ["ko"] = "PUBG를 기다리는 중...",
                ["es"] = "Esperando a PUBG...",
                ["fr"] = "Attente de PUBG...",
                ["de"] = "Warte auf PUBG...",
                ["pt"] = "Aguardando PUBG...",
                ["ru"] = "Ожидание PUBG...",
                ["th"] = "กำลังรอ PUBG...",
                ["vi"] = "Đang chờ PUBG...",
            },
            ["status_detecting"] = new()
            {
                ["en"] = "Detecting PUBG process...",
                ["ja"] = "PUBGプロセスを検出中...",
                ["ko"] = "PUBG 프로세스 감지 중...",
                ["es"] = "Detectando proceso de PUBG...",
                ["fr"] = "Détection du processus PUBG...",
                ["de"] = "Erkennung des PUBG-Prozesses...",
                ["pt"] = "Detectando processo PUBG...",
                ["ru"] = "Обнаружение процесса PUBG...",
                ["th"] = "กำลังตรวจสอบกระบวนการ PUBG...",
                ["vi"] = "Đang phát hiện tiến trình PUBG...",
            },
            ["status_protecting"] = new()
            {
                ["en"] = "Protecting you 💪",
                ["ja"] = "あなたを守っています 💪",
                ["ko"] = "보호 중입니다 💪",
                ["es"] = "Protegiéndote 💪",
                ["fr"] = "Protection active 💪",
                ["de"] = "Wir beschützen dich 💪",
                ["pt"] = "Protegendo você 💪",
                ["ru"] = "Защищаем вас 💪",
                ["th"] = "กำลังปกป้องคุณ 💪",
                ["vi"] = "Đang bảo vệ bạn 💪",
            },
            ["status_stopped"] = new()
            {
                ["en"] = "Monitoring stopped",
                ["ja"] = "監視停止",
                ["ko"] = "모니터링 중지됨",
                ["es"] = "Monitoreo detenido",
                ["fr"] = "Surveillance arrêtée",
                ["de"] = "Überwachung gestoppt",
                ["pt"] = "Monitoramento parado",
                ["ru"] = "Мониторинг остановлен",
                ["th"] = "การตรวจสอบหยุดแล้ว",
                ["vi"] = "Đã dừng giám sát",
            },
            ["status_game_closed"] = new()
            {
                ["en"] = "Game closed · Pausing ✓",
                ["ja"] = "ゲーム終了 · 一時停止中 ✓",
                ["ko"] = "게임 종료 · 일시정지 중 ✓",
                ["es"] = "Juego cerrado · Pausando ✓",
                ["fr"] = "Jeu fermé · Pause ✓",
                ["de"] = "Spiel beendet · Pausiere ✓",
                ["pt"] = "Jogo fechado · Pausando ✓",
                ["ru"] = "Игра закрыта · Пауза ✓",
                ["th"] = "เกมปิด · กำลังหยุดชั่วคราว ✓",
                ["vi"] = "Đã đóng game · Đang tạm dừng ✓",
            },
            ["today_pause"] = new()
            {
                ["en"] = "Today paused", ["ja"] = "本日の一時停止", ["ko"] = "오늘 일시정지",
                ["es"] = "Pausas hoy", ["fr"] = "Pauses aujourd'hui", ["de"] = "Heute pausiert",
                ["pt"] = "Pausas hoje", ["ru"] = "Пауз сегодня", ["th"] = "หยุดวันนี้",
                ["vi"] = "Đã tạm dừng hôm nay", ["id"] = "Jeda hari ini",
            },
            ["saved"] = new()
            {
                ["en"] = "Saved", ["ja"] = "節約", ["ko"] = "절약",
                ["es"] = "Ahorrado", ["fr"] = "Économisé", ["de"] = "Gespart",
                ["pt"] = "Economizado", ["ru"] = "Сэкономлено", ["th"] = "ประหยัด",
                ["vi"] = "Đã tiết kiệm", ["id"] = "Hemat",
            },
            ["minutes"] = new()
            {
                ["en"] = "min", ["ja"] = "分", ["ko"] = "분",
                ["es"] = "min", ["fr"] = "min", ["de"] = "Min",
                ["pt"] = "min", ["ru"] = "мин", ["th"] = "นาที",
                ["vi"] = "phút", ["id"] = "mnt",
            },
            ["btn_start"] = new()
            {
                ["en"] = "▶ Start", ["ja"] = "▶ 開始", ["ko"] = "▶ 시작",
                ["es"] = "▶ Iniciar", ["fr"] = "▶ Démarrer", ["de"] = "▶ Start",
                ["pt"] = "▶ Iniciar", ["ru"] = "▶ Старт", ["th"] = "▶ เริ่ม",
                ["vi"] = "▶ Bắt đầu", ["id"] = "▶ Mulai",
            },
            ["btn_stop"] = new()
            {
                ["en"] = "⏹ Stop", ["ja"] = "⏹ 停止", ["ko"] = "⏹ 중지",
                ["es"] = "⏹ Parar", ["fr"] = "⏹ Arrêter", ["de"] = "⏹ Stopp",
                ["pt"] = "⏹ Parar", ["ru"] = "⏹ Стоп", ["th"] = "⏹ หยุด",
                ["vi"] = "⏹ Dừng", ["id"] = "⏹ Berhenti",
            },
            ["btn_capture"] = new()
            {
                ["en"] = "📷 Capture", ["ja"] = "📷 キャプチャ", ["ko"] = "📷 캡처",
                ["es"] = "📷 Capturar", ["fr"] = "📷 Capturer", ["de"] = "📷 Erfassen",
                ["pt"] = "📷 Capturar", ["ru"] = "📷 Захват", ["th"] = "📷 จับภาพ",
                ["vi"] = "📷 Chụp", ["id"] = "📷 Tangkap",
            },
            ["btn_test"] = new()
            {
                ["en"] = "🔘 Test", ["ja"] = "🔘 テスト", ["ko"] = "🔘 테스트",
                ["es"] = "🔘 Probar", ["fr"] = "🔘 Tester", ["de"] = "🔘 Testen",
                ["pt"] = "🔘 Testar", ["ru"] = "🔘 Тест", ["th"] = "🔘 ทดสอบ",
                ["vi"] = "🔘 Kiểm tra", ["id"] = "🔘 Uji",
            },
            ["btn_check_update"] = new()
            {
                ["en"] = "Check", ["ja"] = "確認", ["ko"] = "확인",
                ["es"] = "Revisar", ["fr"] = "Vérifier", ["de"] = "Prüfen",
                ["pt"] = "Verificar", ["ru"] = "Проверить", ["th"] = "ตรวจสอบ",
                ["vi"] = "Kiểm tra", ["id"] = "Periksa",
            },
            ["btn_clear_log"] = new()
            {
                ["en"] = "Clear", ["ja"] = "消去", ["ko"] = "지우기",
                ["es"] = "Limpiar", ["fr"] = "Effacer", ["de"] = "Löschen",
                ["pt"] = "Limpar", ["ru"] = "Очистить", ["th"] = "ล้าง",
                ["vi"] = "Xóa", ["id"] = "Hapus",
            },
            ["btn_quit"] = new()
            {
                ["en"] = "✕ Quit", ["ja"] = "✕ 終了", ["ko"] = "✕ 종료",
                ["es"] = "✕ Salir", ["fr"] = "✕ Quitter", ["de"] = "✕ Beenden",
                ["pt"] = "✕ Sair", ["ru"] = "✕ Выход", ["th"] = "✕ ออก",
                ["vi"] = "✕ Thoát", ["id"] = "✕ Keluar",
            },
            ["section_coord"] = new()
            {
                ["en"] = "Click Position",
                ["ja"] = "クリック位置設定",
                ["ko"] = "클릭 위치 설정",
                ["es"] = "Configuración de clic",
                ["fr"] = "Configuration du clic",
                ["de"] = "Klick-Position",
                ["pt"] = "Configuração de clique",
                ["ru"] = "Настройка клика",
                ["th"] = "ตั้งค่าตำแหน่งคลิก",
                ["vi"] = "Cài đặt vị trí bấm",
                ["id"] = "Pengaturan klik",
            },
            ["section_coord_desc"] = new()
            {
                ["en"] = "Set the pause button position of Leishen",
                ["ja"] = "雷神加速器の一時停止ボタンの位置を設定",
                ["ko"] = "雷神加速器 일시정지 버튼 위치 설정",
                ["es"] = "Configure la posición del botón de pausa de Leishen",
                ["fr"] = "Définir la position du bouton pause de Leishen",
                ["de"] = "Position des Pause-Buttons von Leishen einstellen",
                ["pt"] = "Defina a posição do botão de pausa do Leishen",
                ["ru"] = "Установите позицию кнопки паузы Leishen",
                ["th"] = "ตั้งค่าตำแหน่งปุ่มหยุดของ Leishen",
                ["vi"] = "Đặt vị trí nút tạm dừng của Leishen",
                ["id"] = "Atur posisi tombol jeda Leishen",
            },
            ["section_options"] = new()
            {
                ["en"] = "Options", ["ja"] = "設定", ["ko"] = "옵션",
                ["es"] = "Opciones", ["fr"] = "Options", ["de"] = "Optionen",
                ["pt"] = "Opções", ["ru"] = "Настройки", ["th"] = "ตัวเลือก",
                ["vi"] = "Tùy chọn", ["id"] = "Opsi",
            },
            ["section_options_desc"] = new()
            {
                ["en"] = "Application behavior",
                ["ja"] = "アプリケーションの動作設定",
                ["ko"] = "앱 동작 설정",
                ["es"] = "Comportamiento de la aplicación",
                ["fr"] = "Comportement de l'application",
                ["de"] = "Anwendungsverhalten",
                ["pt"] = "Comportamento do aplicativo",
                ["ru"] = "Поведение приложения",
                ["th"] = "พฤติกรรมของแอป",
                ["vi"] = "Hành vi ứng dụng",
                ["id"] = "Perilaku aplikasi",
            },
            ["section_log"] = new()
            {
                ["en"] = "Activity Log", ["ja"] = "操作ログ", ["ko"] = "작업 로그",
                ["es"] = "Registro de actividad", ["fr"] = "Journal d'activité",
                ["de"] = "Aktivitätsprotokoll", ["pt"] = "Registro de atividades",
                ["ru"] = "Журнал операций", ["th"] = "บันทึกการดำเนินการ",
                ["vi"] = "Nhật ký hoạt động", ["id"] = "Log aktivitas",
            },
            ["section_log_desc"] = new()
            {
                ["en"] = "Real-time operation log",
                ["ja"] = "リアルタイム操作ログ",
                ["ko"] = "실시간 작업 로그",
                ["es"] = "Registro de operaciones en tiempo real",
                ["fr"] = "Journal d'opérations en temps réel",
                ["de"] = "Echtzeit-Operationsprotokoll",
                ["pt"] = "Registro de operações em tempo real",
                ["ru"] = "Журнал операций в реальном времени",
                ["th"] = "บันทึกการทำงานแบบเรียลไทม์",
                ["vi"] = "Nhật ký hoạt động thời gian thực",
                ["id"] = "Log operasi waktu nyata",
            },
            ["coord_label"] = new()
            {
                ["en"] = "Coord", ["ja"] = "座標", ["ko"] = "좌표",
                ["es"] = "Coord", ["fr"] = "Coordonnées", ["de"] = "Koordinate",
                ["pt"] = "Coord", ["ru"] = "Координаты", ["th"] = "พิกัด",
                ["vi"] = "Tọa độ", ["id"] = "Koordinat",
            },
            ["coord_not_set"] = new()
            {
                ["en"] = "Not set", ["ja"] = "未設定", ["ko"] = "설정 안 됨",
                ["es"] = "No configurado", ["fr"] = "Non défini", ["de"] = "Nicht gesetzt",
                ["pt"] = "Não definido", ["ru"] = "Не задано", ["th"] = "ไม่ได้ตั้งค่า",
                ["vi"] = "Chưa đặt", ["id"] = "Belum diatur",
            },
            ["coord_hint"] = new()
            {
                ["en"] = "💡 Click 'Capture' then click the pause button in Leishen; or press Ctrl+Shift+P",
                ["ja"] = "💡「キャプチャ」をクリックして雷神加速器の一時停止ボタンをクリック、またはCtrl+Shift+P",
                ["ko"] = "💡 '캡처'를 클릭한 후 雷神加速器의 일시정지 버튼을 클릭하거나 Ctrl+Shift+P",
                ["es"] = "💡 Haz clic en 'Capturar' y luego en el botón de pausa de Leishen; o presiona Ctrl+Shift+P",
                ["fr"] = "💡 Cliquez sur 'Capturer' puis sur le bouton pause de Leishen; ou Ctrl+Shift+P",
                ["de"] = "💡 Klicke 'Erfassen' dann den Pause-Button von Leishen; oder Ctrl+Shift+P",
                ["pt"] = "💡 Clique 'Capturar' e depois no botão de pausa do Leishen; ou Ctrl+Shift+P",
                ["ru"] = "💡 Нажмите 'Захват', затем кнопку паузы Leishen; или Ctrl+Shift+P",
                ["th"] = "💡 คลิก 'จับภาพ' แล้วคลิกปุ่มหยุดของ Leishen; หรือกด Ctrl+Shift+P",
                ["vi"] = "💡 Bấm 'Chụp' rồi bấm nút tạm dừng của Leishen; hoặc Ctrl+Shift+P",
                ["id"] = "💡 Klik 'Tangkap' lalu klik tombol jeda Leishen; atau Ctrl+Shift+P",
            },
            ["opt_autostart"] = new()
            {
                ["en"] = "Auto start on boot",
                ["ja"] = "起動時に自動起動",
                ["ko"] = "부팅 시 자동 시작",
                ["es"] = "Inicio automático al arrancar",
                ["fr"] = "Démarrage automatique",
                ["de"] = "Autostart",
                ["pt"] = "Iniciar automaticamente",
                ["ru"] = "Автозапуск",
                ["th"] = "เริ่มอัตโนมัติเมื่อเปิดเครื่อง",
                ["vi"] = "Tự động khởi động",
                ["id"] = "Mulai otomatis saat boot",
            },
            ["opt_reminder"] = new()
            {
                ["en"] = "Show reminder on game exit",
                ["ja"] = "ゲーム終了時に通知を表示",
                ["ko"] = "게임 종료 시 알림 표시",
                ["es"] = "Mostrar recordatorio al salir del juego",
                ["fr"] = "Afficher un rappel à la sortie du jeu",
                ["de"] = "Erinnerung bei Spielende anzeigen",
                ["pt"] = "Mostrar lembrete ao sair do jogo",
                ["ru"] = "Показывать напоминание при выходе из игры",
                ["th"] = "แสดงการแจ้งเตือนเมื่อออกจากเกม",
                ["vi"] = "Hiển thị nhắc nhở khi thoát game",
                ["id"] = "Tampilkan pengingat saat game keluar",
            },
            ["opt_darkmode"] = new()
            {
                ["en"] = "Dark mode",
                ["ja"] = "ダークモード",
                ["ko"] = "다크 모드",
                ["es"] = "Modo oscuro",
                ["fr"] = "Mode sombre",
                ["de"] = "Dunkelmodus",
                ["pt"] = "Modo escuro",
                ["ru"] = "Тёмная тема",
                ["th"] = "โหมดมืด",
                ["vi"] = "Chế độ tối",
                ["id"] = "Mode gelap",
            },
            ["opt_check_update"] = new()
            {
                ["en"] = "Check for updates",
                ["ja"] = "アップデートを確認",
                ["ko"] = "업데이트 확인",
                ["es"] = "Buscar actualizaciones",
                ["fr"] = "Vérifier les mises à jour",
                ["de"] = "Nach Updates suchen",
                ["pt"] = "Verificar atualizações",
                ["ru"] = "Проверить обновления",
                ["th"] = "ตรวจสอบอัปเดต",
                ["vi"] = "Kiểm tra cập nhật",
                ["id"] = "Periksa pembaruan",
            },
            ["log_startup"] = new()
            {
                ["en"] = "App started", ["ja"] = "アプリ起動", ["ko"] = "앱 시작",
                ["es"] = "App iniciada", ["fr"] = "App démarrée", ["de"] = "App gestartet",
                ["pt"] = "App iniciada", ["ru"] = "Приложение запущено", ["th"] = "เริ่มแอป",
                ["vi"] = "Đã khởi động ứng dụng", ["id"] = "Aplikasi dimulai",
            },
            ["log_monitor_started"] = new()
            {
                ["en"] = "▶ Monitoring started",
                ["ja"] = "▶ 監視開始",
                ["ko"] = "▶ 모니터링 시작",
                ["es"] = "▶ Monitoreo iniciado",
                ["fr"] = "▶ Surveillance démarrée",
                ["de"] = "▶ Überwachung gestartet",
                ["pt"] = "▶ Monitoramento iniciado",
                ["ru"] = "▶ Мониторинг запущен",
                ["th"] = "▶ เริ่มการตรวจสอบ",
                ["vi"] = "▶ Đã bắt đầu giám sát",
                ["id"] = "▶ Pemantauan dimulai",
            },
            ["log_monitor_stopped"] = new()
            {
                ["en"] = "⏹ Monitoring stopped",
                ["ja"] = "⏹ 監視停止",
                ["ko"] = "⏹ 모니터링 중지",
                ["es"] = "⏹ Monitoreo detenido",
                ["fr"] = "⏹ Surveillance arrêtée",
                ["de"] = "⏹ Überwachung gestoppt",
                ["pt"] = "⏹ Monitoramento parado",
                ["ru"] = "⏹ Мониторинг остановлен",
                ["th"] = "⏹ หยุดการตรวจสอบ",
                ["vi"] = "⏹ Đã dừng giám sát",
                ["id"] = "⏹ Pemantauan dihentikan",
            },
            ["log_game_detected"] = new()
            {
                ["en"] = "🎮 PUBG detected",
                ["ja"] = "🎮 PUBGを検出",
                ["ko"] = "🎮 PUBG 감지됨",
                ["es"] = "🎮 PUBG detectado",
                ["fr"] = "🎮 PUBG détecté",
                ["de"] = "🎮 PUBG erkannt",
                ["pt"] = "🎮 PUBG detectado",
                ["ru"] = "🎮 PUBG обнаружен",
                ["th"] = "🎮 ตรวจพบ PUBG",
                ["vi"] = "🎮 Đã phát hiện PUBG",
                ["id"] = "🎮 PUBG terdeteksi",
            },
            ["log_game_closed"] = new()
            {
                ["en"] = "🛑 PUBG closed, pausing...",
                ["ja"] = "🛑 PUBG終了、一時停止中...",
                ["ko"] = "🛑 PUBG 종료, 일시정지 중...",
                ["es"] = "🛑 PUBG cerrado, pausando...",
                ["fr"] = "🛑 PUBG fermé, pause...",
                ["de"] = "🛑 PUBG beendet, pausiere...",
                ["pt"] = "🛑 PUBG fechado, pausando...",
                ["ru"] = "🛑 PUBG закрыт, пауза...",
                ["th"] = "🛑 PUBG ปิดแล้ว, กำลังหยุด...",
                ["vi"] = "🛑 PUBG đã đóng, đang tạm dừng...",
                ["id"] = "🛑 PUBG ditutup, menjeda...",
            },
            ["log_pause_success"] = new()
            {
                ["en"] = "✅ Pause successful",
                ["ja"] = "✅ 一時停止成功",
                ["ko"] = "✅ 일시정지 성공",
                ["es"] = "✅ Pausa exitosa",
                ["fr"] = "✅ Pause réussie",
                ["de"] = "✅ Pause erfolgreich",
                ["pt"] = "✅ Pausa bem-sucedida",
                ["ru"] = "✅ Пауза выполнена",
                ["th"] = "✅ หยุดชั่วคราวสำเร็จ",
                ["vi"] = "✅ Tạm dừng thành công",
                ["id"] = "✅ Jeda berhasil",
            },
            ["log_window_found"] = new()
            {
                ["en"] = "✅ Leishen window found",
                ["ja"] = "✅ 雷神加速器のウィンドウを検出",
                ["ko"] = "✅ 雷神加速器 창 발견",
                ["es"] = "✅ Ventana de Leishen encontrada",
                ["fr"] = "✅ Fenêtre Leishen trouvée",
                ["de"] = "✅ Leishen-Fenster gefunden",
                ["pt"] = "✅ Janela do Leishen encontrada",
                ["ru"] = "✅ Окно Leishen найдено",
                ["th"] = "✅ พบหน้าต่าง Leishen",
                ["vi"] = "✅ Đã tìm thấy cửa sổ Leishen",
                ["id"] = "✅ Jendela Leishen ditemukan",
            },
            ["log_window_not_found"] = new()
            {
                ["en"] = "❌ Leishen window not found",
                ["ja"] = "❌ 雷神加速器のウィンドウが見つかりません",
                ["ko"] = "❌ 雷神加速器 창을 찾을 수 없음",
                ["es"] = "❌ Ventana de Leishen no encontrada",
                ["fr"] = "❌ Fenêtre Leishen introuvable",
                ["de"] = "❌ Leishen-Fenster nicht gefunden",
                ["pt"] = "❌ Janela do Leishen não encontrada",
                ["ru"] = "❌ Окно Leishen не найдено",
                ["th"] = "❌ ไม่พบหน้าต่าง Leishen",
                ["vi"] = "❌ Không tìm thấy cửa sổ Leishen",
                ["id"] = "❌ Jendela Leishen tidak ditemukan",
            },
            ["log_auto_click"] = new()
            {
                ["en"] = "📌 Auto-clicking",
                ["ja"] = "📌 自動クリック中",
                ["ko"] = "📌 자동 클릭 중",
                ["es"] = "📌 Auto-clic",
                ["fr"] = "📌 Clic automatique",
                ["de"] = "📌 Automatischer Klick",
                ["pt"] = "📌 Clique automático",
                ["ru"] = "📌 Автоклик",
                ["th"] = "📌 กำลังคลิกอัตโนมัติ",
                ["vi"] = "📌 Đang bấm tự động",
                ["id"] = "📌 Mengklik otomatis",
            },
            ["log_manual_click"] = new()
            {
                ["en"] = "🖱 Manual click",
                ["ja"] = "🖱 手動クリック",
                ["ko"] = "🖱 수동 클릭",
                ["es"] = "🖱 Clic manual",
                ["fr"] = "🖱 Clic manuel",
                ["de"] = "🖱 Manueller Klick",
                ["pt"] = "🖱 Clique manual",
                ["ru"] = "🖱 Ручной клик",
                ["th"] = "🖱 คลิกด้วยตนเอง",
                ["vi"] = "🖱 Bấm thủ công",
                ["id"] = "🖱 Klik manual",
            },
            ["log_update_checking"] = new()
            {
                ["en"] = "Checking for updates...",
                ["ja"] = "アップデートを確認中...",
                ["ko"] = "업데이트 확인 중...",
                ["es"] = "Buscando actualizaciones...",
                ["fr"] = "Vérification des mises à jour...",
                ["de"] = "Suche nach Updates...",
                ["pt"] = "Verificando atualizações...",
                ["ru"] = "Проверка обновлений...",
                ["th"] = "กำลังตรวจสอบอัปเดต...",
                ["vi"] = "Đang kiểm tra cập nhật...",
                ["id"] = "Memeriksa pembaruan...",
            },
            ["log_update_found"] = new()
            {
                ["en"] = "✨ New version found",
                ["ja"] = "✨ 新しいバージョンが見つかりました",
                ["ko"] = "✨ 새 버전 발견",
                ["es"] = "✨ Nueva versión encontrada",
                ["fr"] = "✨ Nouvelle version trouvée",
                ["de"] = "✨ Neue Version gefunden",
                ["pt"] = "✨ Nova versão encontrada",
                ["ru"] = "✨ Найдена новая версия",
                ["th"] = "✨ พบเวอร์ชันใหม่",
                ["vi"] = "✨ Đã tìm thấy phiên bản mới",
                ["id"] = "✨ Versi baru ditemukan",
            },
            ["log_update_latest"] = new()
            {
                ["en"] = "✓ Already up to date",
                ["ja"] = "✓ 最新版です",
                ["ko"] = "✓ 이미 최신 버전",
                ["es"] = "✓ Ya está actualizado",
                ["fr"] = "✓ Déjà à jour",
                ["de"] = "✓ Bereits auf dem neuesten Stand",
                ["pt"] = "✓ Já está atualizado",
                ["ru"] = "✓ Уже обновлено",
                ["th"] = "✓ อัปเดตแล้ว",
                ["vi"] = "✓ Đã là phiên bản mới nhất",
                ["id"] = "✓ Sudah yang terbaru",
            },
            ["log_update_failed"] = new()
            {
                ["en"] = "Update check failed",
                ["ja"] = "アップデート確認失敗",
                ["ko"] = "업데이트 확인 실패",
                ["es"] = "Error al buscar actualizaciones",
                ["fr"] = "Échec de la vérification",
                ["de"] = "Update-Prüfung fehlgeschlagen",
                ["pt"] = "Falha ao verificar atualizações",
                ["ru"] = "Не удалось проверить обновления",
                ["th"] = "ตรวจสอบอัปเดตล้มเหลว",
                ["vi"] = "Kiểm tra cập nhật thất bại",
                ["id"] = "Gagal memeriksa pembaruan",
            },
            ["log_tray_minimized"] = new()
            {
                ["en"] = "Minimized to system tray",
                ["ja"] = "システムトレイに最小化",
                ["ko"] = "시스템 트레이로 최소화",
                ["es"] = "Minimizado a la bandeja del sistema",
                ["fr"] = "Réduit dans la barre d'état système",
                ["de"] = "In die Taskleiste minimiert",
                ["pt"] = "Minimizado para a bandeja do sistema",
                ["ru"] = "Свернуто в системный трей",
                ["th"] = "ย่อไปยังถาดระบบ",
                ["vi"] = "Đã thu nhỏ vào khay hệ thống",
                ["id"] = "Diperkecil ke baki sistem",
            },
            ["theme_dark"] = new()
            {
                ["en"] = "Dark Mode", ["ja"] = "ダークモード", ["ko"] = "다크 모드",
                ["es"] = "Modo oscuro", ["fr"] = "Mode sombre", ["de"] = "Dunkelmodus",
                ["pt"] = "Modo escuro", ["ru"] = "Тёмная тема", ["th"] = "โหมดมืด",
                ["vi"] = "Chế độ tối", ["id"] = "Mode gelap",
            },
            ["theme_light"] = new()
            {
                ["en"] = "Light Mode", ["ja"] = "ライトモード", ["ko"] = "라이트 모드",
                ["es"] = "Modo claro", ["fr"] = "Mode clair", ["de"] = "Hellmodus",
                ["pt"] = "Modo claro", ["ru"] = "Светлая тема", ["th"] = "โหมดสว่าง",
                ["vi"] = "Chế độ sáng", ["id"] = "Mode terang",
            },
            ["reminder_title"] = new()
            {
                ["en"] = "⚠️ Game Exited ⚠️",
                ["ja"] = "⚠️ ゲーム終了 ⚠️",
                ["ko"] = "⚠️ 게임 종료 ⚠️",
                ["es"] = "⚠️ Juego cerrado ⚠️",
                ["fr"] = "⚠️ Jeu fermé ⚠️",
                ["de"] = "⚠️ Spiel beendet ⚠️",
                ["pt"] = "⚠️ Jogo fechado ⚠️",
                ["ru"] = "⚠️ Игра закрыта ⚠️",
                ["th"] = "⚠️ เกมออกแล้ว ⚠️",
                ["vi"] = "⚠️ Đã thoát game ⚠️",
                ["id"] = "⚠️ Game keluar ⚠️",
            },
            ["reminder_subtitle"] = new()
            {
                ["en"] = "PUBG has closed!",
                ["ja"] = "PUBGが終了しました！",
                ["ko"] = "PUBG가 종료되었습니다!",
                ["es"] = "¡PUBG se ha cerrado!",
                ["fr"] = "PUBG s'est fermé !",
                ["de"] = "PUBG wurde beendet!",
                ["pt"] = "PUBG fechou!",
                ["ru"] = "PUBG закрыт!",
                ["th"] = "PUBG ปิดแล้ว!",
                ["vi"] = "PUBG đã đóng!",
                ["id"] = "PUBG telah ditutup!",
            },
            ["reminder_detail"] = new()
            {
                ["en"] = "Leishen has been auto-paused",
                ["ja"] = "雷神加速器を自動一時停止しました",
                ["ko"] = "雷神加速器가 자동 일시정지되었습니다",
                ["es"] = "Leishen se ha pausado automáticamente",
                ["fr"] = "Leishen a été mis en pause automatiquement",
                ["de"] = "Leishen wurde automatisch pausiert",
                ["pt"] = "Leishen foi pausado automaticamente",
                ["ru"] = "Leishen автоматически приостановлен",
                ["th"] = "Leishen หยุดชั่วคราวโดยอัตโนมัติ",
                ["vi"] = "Leishen đã được tự động tạm dừng",
                ["id"] = "Leishen telah dijeda otomatis",
            },
            ["reminder_btn"] = new()
            {
                ["en"] = "✓ Got it", ["ja"] = "✓ わかりました", ["ko"] = "✓ 확인",
                ["es"] = "✓ Entendido", ["fr"] = "✓ Compris", ["de"] = "✓ Verstanden",
                ["pt"] = "✓ Entendi", ["ru"] = "✓ Понятно", ["th"] = "✓ รับทราบ",
                ["vi"] = "✓ Đã hiểu", ["id"] = "✓ Mengerti",
            },
            ["reminder_time"] = new()
            {
                ["en"] = "Paused at", ["ja"] = "一時停止時刻", ["ko"] = "일시정지 시간",
                ["es"] = "Pausado a las", ["fr"] = "Mis en pause à", ["de"] = "Pausiert um",
                ["pt"] = "Pausado às", ["ru"] = "Пауза в", ["th"] = "หยุดเมื่อ",
                ["vi"] = "Đã tạm dừng lúc", ["id"] = "Dijeda pukul",
            },
            ["reminder_today"] = new()
            {
                ["en"] = "Paused today", ["ja"] = "本日の一時停止", ["ko"] = "오늘 일시정지",
                ["es"] = "Pausas hoy", ["fr"] = "Pauses aujourd'hui", ["de"] = "Heute pausiert",
                ["pt"] = "Pausas hoje", ["ru"] = "Пауз сегодня", ["th"] = "หยุดวันนี้",
                ["vi"] = "Đã tạm dừng hôm nay", ["id"] = "Jeda hari ini",
            },
            ["tray_show"] = new()
            {
                ["en"] = "Show Window", ["ja"] = "ウィンドウを表示", ["ko"] = "창 표시",
                ["es"] = "Mostrar ventana", ["fr"] = "Afficher la fenêtre", ["de"] = "Fenster anzeigen",
                ["pt"] = "Mostrar janela", ["ru"] = "Показать окно", ["th"] = "แสดงหน้าต่าง",
                ["vi"] = "Hiển thị cửa sổ", ["id"] = "Tampilkan jendela",
            },
            ["tray_start"] = new()
            {
                ["en"] = "Start Monitor", ["ja"] = "監視開始", ["ko"] = "모니터링 시작",
                ["es"] = "Iniciar monitoreo", ["fr"] = "Démarrer", ["de"] = "Überwachung starten",
                ["pt"] = "Iniciar monitoramento", ["ru"] = "Запустить мониторинг",
                ["th"] = "เริ่มตรวจสอบ", ["vi"] = "Bắt đầu giám sát", ["id"] = "Mulai pemantauan",
            },
            ["tray_stop"] = new()
            {
                ["en"] = "Stop Monitor", ["ja"] = "監視停止", ["ko"] = "모니터링 중지",
                ["es"] = "Detener monitoreo", ["fr"] = "Arrêter", ["de"] = "Überwachung stoppen",
                ["pt"] = "Parar monitoramento", ["ru"] = "Остановить мониторинг",
                ["th"] = "หยุดตรวจสอบ", ["vi"] = "Dừng giám sát", ["id"] = "Hentikan pemantauan",
            },
            ["tray_quit"] = new()
            {
                ["en"] = "Quit", ["ja"] = "終了", ["ko"] = "종료",
                ["es"] = "Salir", ["fr"] = "Quitter", ["de"] = "Beenden",
                ["pt"] = "Sair", ["ru"] = "Выход", ["th"] = "ออก",
                ["vi"] = "Thoát", ["id"] = "Keluar",
            },
            ["tray_balloon"] = new()
            {
                ["en"] = "Minimized to tray, running in background",
                ["ja"] = "トレイに最小化、バックグラウンドで実行中",
                ["ko"] = "트레이로 최소화, 백그라운드에서 실행 중",
                ["es"] = "Minimizado, ejecutándose en segundo plano",
                ["fr"] = "Réduit dans la barre, exécution en arrière-plan",
                ["de"] = "In Taskleiste minimiert, läuft im Hintergrund",
                ["pt"] = "Minimizado, executando em segundo plano",
                ["ru"] = "Свернуто в трей, работает в фоне",
                ["th"] = "ย่อไปถาด ทำงานในพื้นหลัง",
                ["vi"] = "Đã thu nhỏ vào khay, đang chạy nền",
                ["id"] = "Diperkecil ke baki, berjalan di latar belakang",
            },
            ["footer_copyright"] = new()
            {
                ["en"] = "© Designed by BadeGusi",
                ["ja"] = "© 巴德古ス 設計",
                ["ko"] = "© 바드구스 디자인",
                ["es"] = "© Diseñado por BadeGusi",
                ["fr"] = "© Conçu par BadeGusi",
                ["de"] = "© Entworfen von BadeGusi",
                ["pt"] = "© Projetado por BadeGusi",
                ["ru"] = "© Разработано BadeGusi",
                ["th"] = "© ออกแบบโดย BadeGusi",
                ["vi"] = "© Thiết kế bởi BadeGusi",
                ["id"] = "© Dirancang oleh BadeGusi",
            },
            ["footer_stats"] = new()
            {
                ["en"] = "Started {0} · Paused {1} times",
                ["ja"] = "起動 {0} 回 · 一時停止 {1} 回",
                ["ko"] = "시작 {0}회 · 일시정지 {1}회",
                ["es"] = "Iniciado {0} · Pausado {1} veces",
                ["fr"] = "Démarré {0} · Mis en pause {1} fois",
                ["de"] = "Gestartet {0} · Pausiert {1} mal",
                ["pt"] = "Iniciado {0} · Pausado {1} vezes",
                ["ru"] = "Запущен {0} · Пауз {1}",
                ["th"] = "เริ่ม {0} · หยุด {1} ครั้ง",
                ["vi"] = "Đã khởi động {0} · Đã tạm dừng {1} lần",
                ["id"] = "Dimulai {0} · Dijeda {1} kali",
            },
        };

        // 中文翻译（默认）
        public static readonly Dictionary<string, string> Zh = new()
        {
            ["app_title"] = "PUBG助手 - 智能时长暂停工具",
            ["app_subtitle"] = "智能时长暂停工具 · v2.0",
            ["app_version"] = "v2.0",
            ["status_label"] = "状态",
            ["status_idle"] = "未运行",
            ["status_scanning"] = "扫描中",
            ["status_gaming"] = "游戏中",
            ["status_paused"] = "已暂停",
            ["status_waiting"] = "等待 PUBG 启动...",
            ["status_detecting"] = "正在检测 PUBG 进程...",
            ["status_protecting"] = "正在为你保驾护航 💪",
            ["status_stopped"] = "监控已停止",
            ["status_game_closed"] = "检测到游戏关闭 · 执行暂停 ✓",
            ["today_pause"] = "今日暂停",
            ["saved"] = "节省",
            ["minutes"] = "分钟",
            ["btn_start"] = "▶ 开始监控",
            ["btn_stop"] = "⏹ 停止监控",
            ["btn_capture"] = "📷 捕获坐标",
            ["btn_test"] = "🔘 测试点击",
            ["btn_check_update"] = "检查",
            ["btn_clear_log"] = "清空",
            ["btn_quit"] = "✕ 退出程序",
            ["section_coord"] = "暂停设置",
            ["section_coord_desc"] = "配置雷神加速器暂停按钮位置",
            ["section_options"] = "选项设置",
            ["section_options_desc"] = "应用行为配置",
            ["section_log"] = "操作日志",
            ["section_log_desc"] = "实时记录所有操作",
            ["coord_label"] = "坐标",
            ["coord_not_set"] = "未设置",
            ["coord_hint"] = "💡 点击「捕获坐标」然后点击雷神加速器的暂停按钮；或按 Ctrl+Shift+P 直接捕获",
            ["opt_autostart"] = "开机自动启动",
            ["opt_reminder"] = "游戏退出时弹窗提醒",
            ["opt_darkmode"] = "深色模式",
            ["opt_check_update"] = "检查更新",
            ["log_startup"] = "程序启动",
            ["log_monitor_started"] = "▶ 监控已启动",
            ["log_monitor_stopped"] = "⏹ 监控已停止",
            ["log_game_detected"] = "🎮 检测到 PUBG 启动",
            ["log_game_closed"] = "🛑 PUBG 已关闭，执行暂停操作",
            ["log_pause_success"] = "✅ 暂停成功",
            ["log_coord_captured"] = "✅ 坐标捕获成功",
            ["log_window_found"] = "✅ 找到雷神加速器窗口",
            ["log_window_not_found"] = "❌ 未找到雷神加速器窗口",
            ["log_auto_click"] = "📌 尝试自动点击",
            ["log_manual_click"] = "🖱 使用坐标模拟点击",
            ["log_update_checking"] = "正在检查更新...",
            ["log_update_found"] = "✨ 发现新版本",
            ["log_update_latest"] = "✓ 已是最新版本",
            ["log_update_failed"] = "检查更新失败",
            ["log_tray_minimized"] = "最小化到系统托盘",
            ["theme_dark"] = "深色模式",
            ["theme_light"] = "亮色模式",
            ["lang_zh"] = "中文",
            ["lang_en"] = "English",
            ["reminder_title"] = "⚠️ 游戏已退出 ⚠️",
            ["reminder_subtitle"] = "PUBG 已经关闭！",
            ["reminder_detail"] = "已自动暂停雷神加速器",
            ["reminder_btn"] = "✓ 知道了",
            ["reminder_time"] = "暂停时间",
            ["reminder_today"] = "今日已暂停",
            ["update_title"] = "发现更新",
            ["update_msg"] = "发现新版本 {0}！\n\n{1}\n\n是否下载更新？",
            ["tray_show"] = "显示主窗口",
            ["tray_start"] = "开始监控",
            ["tray_stop"] = "停止监控",
            ["tray_quit"] = "退出",
            ["tray_balloon"] = "已最小化到系统托盘，将继续后台运行",
            ["footer_copyright"] = "© 巴德古斯 设计",
            ["footer_qq"] = "QQ: 2994938720",
            ["footer_stats"] = "累计启动 {0} 次 · 暂停 {1} 次",
        };

        // 英文翻译（fallback）
        public static readonly Dictionary<string, string> En = new()
        {
            ["app_title"] = "PUBG Monitor",
            ["app_subtitle"] = "Smart Pause Tool · v2.0",
            ["app_version"] = "v2.0",
            ["status_label"] = "Status",
            ["status_idle"] = "Idle",
            ["status_scanning"] = "Scanning",
            ["status_gaming"] = "In Game",
            ["status_paused"] = "Paused",
            ["status_waiting"] = "Waiting for PUBG...",
            ["status_detecting"] = "Detecting PUBG process...",
            ["status_protecting"] = "Protecting you 💪",
            ["status_stopped"] = "Monitoring stopped",
            ["status_game_closed"] = "Game closed · Pausing ✓",
            ["today_pause"] = "Today paused",
            ["saved"] = "Saved",
            ["minutes"] = "min",
            ["btn_start"] = "▶ Start",
            ["btn_stop"] = "⏹ Stop",
            ["btn_capture"] = "📷 Capture",
            ["btn_test"] = "🔘 Test",
            ["btn_check_update"] = "Check",
            ["btn_clear_log"] = "Clear",
            ["btn_quit"] = "✕ Quit",
            ["section_coord"] = "Click Position",
            ["section_coord_desc"] = "Set pause button position",
            ["section_options"] = "Options",
            ["section_options_desc"] = "Application behavior",
            ["section_log"] = "Activity Log",
            ["section_log_desc"] = "Real-time operation log",
            ["coord_label"] = "Coord",
            ["coord_not_set"] = "Not set",
            ["coord_hint"] = "💡 Click 'Capture' then click the pause button; or press Ctrl+Shift+P",
            ["opt_autostart"] = "Auto start on boot",
            ["opt_reminder"] = "Show reminder on game exit",
            ["opt_darkmode"] = "Dark mode",
            ["opt_check_update"] = "Check for updates",
            ["log_startup"] = "App started",
            ["log_monitor_started"] = "▶ Monitoring started",
            ["log_monitor_stopped"] = "⏹ Monitoring stopped",
            ["log_game_detected"] = "🎮 PUBG detected",
            ["log_game_closed"] = "🛑 PUBG closed, pausing...",
            ["log_pause_success"] = "✅ Pause successful",
            ["log_coord_captured"] = "✅ Coordinates captured",
            ["log_window_found"] = "✅ Leishen window found",
            ["log_window_not_found"] = "❌ Leishen window not found",
            ["log_auto_click"] = "📌 Auto-clicking",
            ["log_manual_click"] = "🖱 Manual click",
            ["log_update_checking"] = "Checking for updates...",
            ["log_update_found"] = "✨ New version found",
            ["log_update_latest"] = "✓ Already up to date",
            ["log_update_failed"] = "Update check failed",
            ["log_tray_minimized"] = "Minimized to system tray",
            ["theme_dark"] = "Dark Mode",
            ["theme_light"] = "Light Mode",
            ["lang_zh"] = "中文",
            ["lang_en"] = "English",
            ["reminder_title"] = "⚠️ Game Exited ⚠️",
            ["reminder_subtitle"] = "PUBG has closed!",
            ["reminder_detail"] = "Leishen has been auto-paused",
            ["reminder_btn"] = "✓ Got it",
            ["reminder_time"] = "Paused at",
            ["reminder_today"] = "Paused today",
            ["update_title"] = "Update Available",
            ["update_msg"] = "New version {0} found!\n\n{1}\n\nDownload now?",
            ["tray_show"] = "Show Window",
            ["tray_start"] = "Start Monitor",
            ["tray_stop"] = "Stop Monitor",
            ["tray_quit"] = "Quit",
            ["tray_balloon"] = "Minimized to tray, running in background",
            ["footer_copyright"] = "© Designed by BadeGusi",
            ["footer_qq"] = "QQ: 2994938720",
            ["footer_stats"] = "Started {0} · Paused {1} times",
        };
    }
}
