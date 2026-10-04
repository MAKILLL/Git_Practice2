# Unity Profiling Lab

Учебный проект к лекции 2: Development Build, CPU/GPU/Memory/Frame Time и диагностика захвата в Timeline/Hierarchy.

- Unity: `6000.3.16f1`.
- Сцена: `Assets/Scenes/ProfilingLab.unity`.
- Проект подготовлен для самостоятельной Development Build; папка `Build/` может отсутствовать в учебном ZIP.
- CPU-маркер: `Lesson.RebuildRoutes`.
- Нагрузка: 80 детерминированных поисков маршрута по сетке каждые 120 кадров. Это реальное вычисление на главном потоке, без `Sleep`.
- Эталонный запуск на Ryzen 7 4800H / GTX 1650 / 1280x720: 5 пиков, среднее измерение участка `59,06 ms`. На другом компьютере сравнивайте структуру и период пика, а не это абсолютное число.

## Воспроизведение базовой линии

1. Откройте проект в Unity 6000.3.16f1.
2. Откройте `Assets/Scenes/ProfilingLab.unity`.
3. Для проверки сцены можно включить Play Mode. Для отчётных измерений запускайте только собранный Development Player, подключённый к Profiler.
4. Между пиками сцена ограничена `Application.targetFrameRate = 60`; записывайте стабильный Frame Time отдельно от кадров с пиком.
5. Сравнивайте результаты только при одинаковом разрешении, качестве, сцене и длительности записи. Абсолютные миллисекунды зависят от CPU/GPU компьютера.

## Новый Development Build с Auto Connect Profiler

1. Откройте `File > Build Profiles`.
2. Выберите Windows и сцену `ProfilingLab`.
3. Включите `Development Build` и `Autoconnect Profiler`. `Script Debugging` для этой практики не нужен.
4. Нажмите `Build And Run`.
5. В `Window > Analysis > Profiler` выберите запущенный Player, а не Editor.

Собирайте Player с включёнными `Development Build` и `Autoconnect Profiler`, с выключенными `Script Debugging` и `Deep Profiling Support`. Нужный участок уже отмечен `ProfilerMarker`.

## Практика 2.2: supplied capture

1. Откройте `Window > Analysis > Profiler`.
2. Нажмите кнопку загрузки и выберите `../../captures/Lecture2_DevelopmentPlayer.raw`.
3. В CPU Usage найдите высокий кадр и переключитесь на `Timeline`.
4. Раскройте `Main Thread > PlayerLoop > Update.ScriptRunBehaviourUpdate` и найдите `Lesson.RebuildRoutes`.
5. Переключитесь на `Hierarchy`, включите сортировку по `Total ms` или `Self ms` и найдите тот же маркер.
6. Сравните высокий кадр с соседним обычным кадром. Пик повторяется каждые 120 кадров; постоянной нагрузки быть не должно.

Ожидаемый диагноз: захват доказывает существенный CPU-вклад периодической перестройки маршрутов на Main Thread. В supplied capture нет пригодных GPU-сэмплов, поэтому по нему нельзя исключить отдельную GPU-проблему. Число миллисекунд машинозависимо, а имя маркера, поток, период и форма CPU-пика воспроизводимы.

## Память и GPU

- Memory: после прогрева используйте `Total Used Memory` как базовую линию; алгоритм повторно использует массивы и не должен давать заметный GC Alloc на каждом пике.
- GPU: смотрите GPU Usage только если платформа и графический API поддерживают GPU-профилирование. Отсутствие GPU-модуля не меняет CPU-диагноз supplied capture.
- Записывайте: Unity version, Development Build, разрешение, CPU/GPU, средний обычный Frame Time и длительность выбранного пика.
