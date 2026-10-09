# Перенос исходников PR #2 в CannibalRally на Windows 11

Архив `releases/cannibal-rally-modapi-source-only.zip` содержит шесть C#-файлов и эту инструкцию. Это архив для переноса исходников, **не пакет для ModAPI или Melty и не готовый мод**. Файлы взяты из PR #2, commit `8343af4a5f807cc8c18f3a0f09a053d7da7a61af`.

## 1. Сохраните существующий проект

Закройте Visual Studio и сделайте копию папки `Desktop\mod\projects\TheForest\CannibalRally`. Существующие `CannibalRally.csproj`, `.sln` и `ModInfo.xml` оставьте на месте. Не заменяйте их файлами из другого проекта.

## 2. Скопируйте только исходники

Распакуйте архив. Скопируйте папку **CannibalRallySource целиком** в папку проекта. Должна получиться структура:

```text
Desktop\mod\projects\TheForest\CannibalRally\
  CannibalRally.csproj           (ваш существующий файл)
  ModInfo.xml                   (ваш существующий файл)
  CannibalRallySource\
    Core\
      RallyState.cs
      SidecarStore.cs
    Runtime\
      BuggyFactory.cs
      BuggyVehicle.cs
      DriverSession.cs
      RallyMod.cs
```

`native/Tests`, `CoreTests.csproj`, браузерные `src`, `index.html`, npm-файлы и старый веб-ZIP копировать не нужно. `config.example.xml` для компиляции не нужен: runtime создаёт собственный config при первом F6.

## 3. Проверьте зависимости в Visual Studio

Откройте существующую `.sln`; если её нет — `CannibalRally.csproj`. В обозревателе решений разверните «Ссылки» (References). Проверьте, что ссылки не помечены предупреждением и их свойство Path указывает на существующие DLL вашего ModAPI/игры:

- `BaseModLib` — библиотека вашего ModAPI;
- `UnityEngine`, `Assembly-CSharp`, `Assembly-CSharp-firstpass`, `bolt` — ссылки из ModAPI-проекта для установленной The Forest;
- стандартные `System`, `System.Core`, `System.Xml` — для коллекций и XML.

Это проверка ожидаемых зависимостей исходников; ваш локальный `.csproj` в облачной среде недоступен, поэтому его корректность не подтверждена. Дополнительные ссылки generated project сохраняйте. Если ссылка отсутствует или не разрешается, сначала исправьте путь к игре в ModAPI и создайте отдельный временный проект The Forest для сравнения references. Не подставляйте скачанные игровые DLL или придуманные HintPath.

Сохраните TargetFrameworkVersion, AssemblyName, OutputPath, ModAPI references, HintPath и ModInfo вашего проекта. .NET 8 используется только тестами общей логики в CI; переводить игровой проект на net8.0 не нужно. Namespace `CannibalRally` в исходниках не требует переименования проекта.

## 4. Добавьте файлы в проект

Простое копирование файлов обычно не включает их в сборку старого ModAPI `.csproj`. В Visual Studio включите «Показать все файлы» (Show All Files), найдите `CannibalRallySource`, нажмите правой кнопкой и выберите «Включить в проект» (Include In Project). Убедитесь, что все шесть `.cs` включены и их Build Action — Compile. Если эта команда недоступна, создайте в проекте папки CannibalRallySource/Core и CannibalRallySource/Runtime и через «Добавить → Существующий элемент» выберите соответствующие файлы из уже скопированных папок.

Visual Studio обновит существующий `.csproj`, сохранив ссылки. Если добавляете вручную, после проверки зависимостей достаточно **добавить** внутри `<Project>` следующий ItemGroup — и только если этих Compile entries ещё нет:

```xml
<ItemGroup>
  <Compile Include="CannibalRallySource\Core\RallyState.cs" />
  <Compile Include="CannibalRallySource\Core\SidecarStore.cs" />
  <Compile Include="CannibalRallySource\Runtime\BuggyFactory.cs" />
  <Compile Include="CannibalRallySource\Runtime\BuggyVehicle.cs" />
  <Compile Include="CannibalRallySource\Runtime\DriverSession.cs" />
  <Compile Include="CannibalRallySource\Runtime\RallyMod.cs" />
</ItemGroup>
```

Другие sections не меняйте. В project с wildcard Compile entries отдельное добавление может не понадобиться; решающая проверка — включены ли эти шесть файлов в сборку без дублирования. Если в вашем проекте уже есть собственный startup/spawn Cannibal Rally, сначала проверьте, что он не создаёт второй экземпляр: в новых исходниках точка входа — `RallyMod.Initialize` с ExecuteOnGameStart.

**Не запускайте `scripts/prepare_modapi.py` из PR без адаптации:** он принимает AssemblyName `CannibalRallyExperimental` и отклонит ваш `CannibalRally`. Переименовывать проект ради этого скрипта не нужно.

## 5. Соберите и проверьте в игре

Выберите Release и «Сборка → Собрать решение» (Build Solution). При ошибке типа/члена Unity или TheForest сначала проверьте dependencies и версию игры; совместимость runtime ещё не подтверждена. Не обходите ошибки заглушками. После успешной сборки откройте проект в ModAPI, нажмите Create Mod, включите созданный `.mod`, затем Start Game.

В одиночном мире без VR и других модов встаньте на открытом ровном участке: F6 — создать/восстановить багги, F7 — посадка/выход, WASD — движение, Space — тормоз. F5 — сбор ящика пешком, F9 — ремонт, F10 — заправка, F8 — отдельное сохранение припаркованной машины после выхода. Сохранение The Forest выполняется отдельно.

Проверено в CI: 22 assertions общей C#-логики и три теста подготовки проекта. Полная сборка на игровых DLL и запуск в The Forest не проверены. Этот архив нельзя импортировать в Melty как playable release.
