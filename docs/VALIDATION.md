# Проверки native-кандидата — 9 октября 2026

## Выполнено в облачной среде

- Актуальные исходники GitHub и ModAPI docs/API examples изучены.
- `python scripts/check_native.py`: source/config layout PASS, обнаружены отсутствующий C# compiler, четыре игровых reference DLL и BaseModLib. Статусы native build / in-game / Melty остаются false.
- `python -m unittest discover -s native/Tests -p 'test_*.py' -v`: 3 теста PASS. Проверены отсутствие ложной release readiness, подготовка generated project без замены framework/hint paths, повторная подготовка без duplicate Compile, backup и отказ менять чужой проект.
- `python scripts/check_native.py --require-build`: ожидаемый ненулевой exit code из-за отсутствующих prerequisites. Это блокировка, а не успешная сборка.

Общий C# core скомпилирован и протестирован GitHub Actions (.NET 8): **22 assertions PASS**, Python tests **3 PASS**, workflow **success**. [Проверенный запуск CI](https://github.com/Ftor69/Cannibal-Rally/actions/runs/37907825130) для commit `13eddecd3025941173cf715842e534e4e5392ea5`. Локально `dotnet` отсутствует. В shared XML-коде есть предупреждение CS0618 для `ProhibitDtd`: этот API оставлен ради старого Mono/.NET; тест DTD rejection прошёл. Framework native-проекта определяется ModAPI, а .NET 8 не является target runtime мода. Core runner проверяет depletion, damage, fuel conservation, service restrictions, pickup deduplication, save schema/numeric validation, profile path safety, XML roundtrip, backup replacement и DTD rejection. Он не проверяет Unity runtime.

Сборка ModAPI-библиотеки **не выполнена**. `.mod` **не создан**. The Forest **не запускалась**. `inspect_package`, `validate_recipe`, `one_click_check` **не выполнены**. Melty ZIP, release и screenshot native-версии **не созданы**. Старый веб-архив не переупаковывался.

## Обязательная проверка на игровой машине

| Сценарий | Что должно быть проверено | Статус |
|---|---|---|
| Generated project build / Create Mod | Все реальные references и API компилируются, ModAPI создаёт `.mod` | Не проверено |
| Загрузка мира / F6 | Один багги, нет повторного spawn, start/save позиция не внутри препятствия | Не проверено |
| WASD / тормоз / подвеска | Устойчивое движение, колёса касаются поверхности, нет провала/разлёта | Не проверено |
| F7 вход/выход | Native rig не дёргается; пешие controls/camera/collision states восстанавливаются | Не проверено |
| Тесный выход / склон | Выход отклоняется; машина остаётся управляемой | Не проверено |
| Меню / пауза / focus loss | Машина тормозит, нет непреднамеренного движения | Не проверено |
| Удар о дерево/камень | Реальный collision и разумный урон, native объекты не ломаются самовольно | Не проверено |
| Бак пуст / прочность 0 | Тяга отключена; fuel/repair реально восстанавливают работу | Не проверено |
| Ящики и ремонт | 5 л / одна деталь; нет повторного сбора, debit только при успешном действии | Не проверено |
| Штатные каннибалы | Видят движущегося пассажира, преследуют и наносят native урон; vehicle damage отслеживает здоровье | Не проверено |
| Смерть / выход в меню / новая игра | Нет оставленных отключённых компонентов, чужих объектов или material leaks | Не проверено |
| F8 / reload / config profile | Возврат позы, ресурсов и pickup ledger; native save не изменяется | Не проверено |
| Corrupt XML / bad config | Fail closed, прежнее сохранение не перезаписано; backup восстановим | Не проверено |
| Multiplayer | При Bolt-сессии мод не создаёт и удаляет свою машину; не заявляется сетевой gameplay | Не проверено |
| Melty clean install / Play | Установка, loader, запуск, rollback с настоящим валидированным recipe | Не проверено |

Особые риски: historical LocalPlayer API может измениться; native rig pivot/камера могут отличаться; каннибалам может потребоваться отдельная интеграция; по снижению Health нельзя определить источник урона; крыши/вода/пещеры не классифицируются по игровым API; боковой выход проверяет только Unity-геометрию. XML sidecar не синхронизирован с native save slot/rollback. Не выдавайте эти пункты за тесты, которые прошли.
