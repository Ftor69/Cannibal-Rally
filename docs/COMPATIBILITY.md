# Подтверждённые возможности и ограничения — 9 октября 2026

## Репозиторий

Исследована актуальная ветка GitHub `main`, исходный commit `6e5245fb92077c8db342a162a023b4c0b091e60c`. Это Three.js/cannon-es/Vite веб-прототип, не мод. В локальном старом checkout находился только начальный README; для работы получены актуальные текстовые файлы через GitHub connector. AGENTS.md, SDK и нативных исходников в актуальном дереве не было.

## Melty

[Официальная страница](https://melty.gg/) подтверждает установку и запуск мешапов и получение инструкций для coding agent. Пользователь предоставил Publish instructions: MCP endpoint `https://melty.gg/api/mcp`, методы `mod_status`, `list_my_mods`, `search_games`, `game_info`, `inspect_package`, `validate_recipe`, `one_click_check`, загрузку и публикацию. Это подтверждает рабочий процесс, но не схему аргументов инструментов и не поддержку The Forest/ModAPI.

Черновик пользователя: `ac0b8860-c43e-4624-a98d-063ad1cb068e`. Его существование, текущий статус и файлы не проверены на сервере. Melty MCP не подключён среди инструментов среды. HTTP-проверка endpoint завершилась `Failed to connect to proxy port 8080`; такая же проблема возникла на GitHub clone. Аутентификация не выполнялась, токен не проверен. Ограниченная сеть не включает melty.gg в список разрешённых направлений. Прокси не обходился, настройки агента не менялись.

Не выполнены `game_info`, `inspect_package`, `validate_recipe`, `one_click_check`. Неизвестны: допустимый loader для The Forest, его версия, структура компонента, installation destinations и launch recipe. Поэтому Melty ZIP/recipe не создаётся. Melty и MelonLoader — разные продукты; формат MelonLoader не подменяет контракт Melty.

## Реальный путь моддинга The Forest

[Официальная документация ModAPI](https://modapi.survivetheforest.net/documentation/) описывает C#-проект, `ModAPI.Attributes.ExecuteOnGameStart`, добавление MonoBehaviour в сцену, `TheForest.Utils.LocalPlayer`, игровые DLL и создание `.mod` кнопкой Create Mod. Сборка библиотеки сама по себе не создаёт установочный `.mod`.

[Конфигурация The Forest в ModAPI](https://github.com/FluffyFishGames/ModAPI/blob/master/ModAPI/configs/games/TheForest.xml) перечисляет `UnityEngine.dll`, `Assembly-CSharp.dll`, `Assembly-CSharp-firstpass.dll`, `bolt.dll` и Steam App ID 242760. [Исходники ModProject](https://github.com/FluffyFishGames/ModAPI/blob/master/ModAPI/Data/Models/ModProject.cs) показывают реальные generated references, XML ModInfo и упаковку. Разные версии ModAPI генерируют разные framework targets: `prepare_modapi.py` сохраняет framework, hint paths и metadata именно вашего проекта.

Для проверки существующих членов `LocalPlayer.Transform`, `FpCharacter`, `CamFollowHead`, `CamRotator`, `MainRotator`, `Stats.Health`, `FpCharacter.Locked` и `BoltNetwork.isRunning` изучен [опубликованный код мода Hellsing](https://github.com/Hellsing/ModAPI-Mods/blob/master/GriefClientPro/GriefClientPro.cs). Его код не копируется и его функции не включаются. Это исторический пример API, а не гарантия совместимости с конкретной установленной версией. Прямая компиляция против вашей игры должна подтвердить все типы и члены.

Unity API: [WheelCollider 5.6](https://docs.unity3d.com/560/Documentation/ScriptReference/WheelCollider.html), [Physics.CheckCapsule](https://docs.unity3d.com/560/Documentation/ScriptReference/Physics.CheckCapsule.html), [Application.persistentDataPath](https://docs.unity3d.com/560/Documentation/ScriptReference/Application-persistentDataPath.html). WheelCollider — штатная подвеска и контактная модель Unity; точная версия Unity установленной игры здесь не определена.

Нет предоставленной игры, Steam, game DLLs или BaseModLib.dll. В среде отсутствуют dotnet/mcs/msbuild. .NET 8 тестирует только общий C# core, не Mono-runtime и не игровые API. DLL-заглушки не используются для выдачи проверки за настоящую сборку.

## Объединение с Forza

Подтверждённого моста к Forza Horizon 5 нет. Реализуются оригинальные механики в The Forest. Forza не должна быть обязательной игрой в listing или рецепте: её ресурсы, двоичные файлы и игра не используются. Установленная легальная копия The Forest нужна для refs и runtime; её DLL не распространяются.

## Интеграции, которые нельзя считать завершёнными

Штатные каннибалы не заменяются. Игрок следует за багги, оставаясь целью game AI; срабатывание pursuit/attack требует проверки в игре. Потеря здоровья пассажира дополнительно повреждает машину, включая любые причины (бой, голод и другие): это не hook конкретной атаки. Не реализованы наезд с нанесением урона врагу, целевое разрушение построек/деревьев, native inventory fuel recipes, защита/синхронизация нативного save slot, multiplayer и VR.

Sidecar сохранения используют пользовательский профиль. Они не изменяют native save и не сохраняют время/врагов/позицию пешего игрока. Возвращаясь к более раннему native save, пользователь может получить несогласованную машину/ресурсы: это не атомарная часть сохранения игры.
