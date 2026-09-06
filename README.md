# Speech-Synthesizer-MiDi-Keyboard

Egy Windows-alkalmazás, amely a beépített Microsoft beszédszintetizátorokat
(**SAPI5**, **OneCore**, és — korlátozásokkal, lásd lent — **SAPI4**) MIDI-billentyűzetszerű
"éneklő" eszközzé alakítja: gépi billentyűzeten beütött szótagok/betűk a
megadott hangmagasságon szólalnak meg, NVDA-kompatibilis, telepítés nélküli
(portable) felületen.

## Ihletforrások

- **Gakuen Pocket Miku** — hordozható, billentyűzettel vezérelt énekszintetizátor-játék.
- [VocalWriter](https://github.com/masonasons/vocalwriter/) — szöveg alapú, billentyűzettel vezérelt hangszintetizátor eszköz.
- A JavaScript azon képessége, hogy böngészőben billentyűlenyomásra tud
  hangokat (pl. japán szótagokat) rendelni billentyűkhöz, és azokat kimondani.
- **UTAU** énekhang-szintetizátor.
- [infoalap.hu — Hallhassam / DEX](https://infoalap.hu/megoldasok/hallhassam/dex/) megoldás.

## Funkciók

- Beszédhang-motorok: Microsoft **OneCore**, **SAPI5**, és (detektálás szintjén) **SAPI4**.
- **Beállítások** ablak: motor, hang, sebesség, hangerő kiválasztása.
- **Ctrl+K**: megnyitja a MIDI-billentyűzet overlay-t, **Escape**-kel zárható.
- A MIDI-billentyűzeten belül:
  - `Z S X D C V G B H N J M , L .` — hangok lejátszása (a szótag/betű puffer
    tartalma szólal meg az adott hangmagasságon).
  - **Jobb Shift** (nyomva tartva): hangmagasság-eltolás (pitch bend) felfelé.
  - **Bal Shift** (nyomva tartva): hangmagasság-eltolás lefelé.
  - **0-9**: oktáv váltás.
  - **F2-F12**: a pitch bend "pontjának" (mértékének) kiválasztása — 11 fokozat.
- A billentyűzeten kívül: **Ctrl+1** szótagot, **Ctrl+2** betűt ad hozzá a pufferhez.
- Minden funkció elérhető **Alt**-tal aktiválható menüből is, billentyűparancsok
  a menüben feltüntetve.
- Indításkor a program ellenőrzi, hogy a SAPI5/OneCore/SAPI4 motorok és hangok
  telepítve vannak-e; ha nem, felajánlja a hiányzók telepítését (lásd a
  "Motorok telepítése" korlátozásait lent).
- **NVDA-kompatibilis**: kizárólag natív WinForms vezérlőket használ
  (Label, Button, ComboBox, MenuStrip, stb.), amelyeket az NVDA UI Automation/MSAA
  révén natívan felolvas; nincsenek egyedi rajzolású (owner-draw) elemek.
- **Portable**: nincs telepítő, nincs regisztrytartós adat — minden beállítás
  (`settings.json`, `pitch_default.json`) a program mappájában, a program
  mellett tárolódik.

## Hangmagasság-kezelés (spec 8. pont)

Az alkalmazás első indításakor elmenti az aktuális (alap) sebesség/hangerő/
hangmagasság értékeket a `pitch_default.json` fájlba. A MIDI-billentyűzet
használata közben a program csak a *lejátszott hangokra* alkalmaz eltérő
hangmagasságot (SSML `<prosody pitch="...">` révén) — amikor a billentyűzetet
**Escape**-kel bezárod, a program visszaállítja a mentett alapértékeket, így a
normál (nem "éneklő") beszéd utána a megszokott hangon folytatódik.

## Fontos, őszinte korlátozások

- **SAPI4**: a Microsoft ezt a motort a Windows XP kora óta nem terjeszti
  hivatalosan, nincs hozzá letölthető/telepíthető csomag Windows 10/11 alá, és
  a régi 32-bites SAPI4 hangmotorok nem futtathatók natívan 64-bites
  folyamatban. A program ezért **csak detektálja** a gépen esetleg megmaradt
  régi SAPI4-regisztrációkat, de nem tud rá valódi telepítést vagy lejátszást
  kínálni. Ez szándékos, dokumentált korlátozás, nem hiba.
- **OneCore/SAPI5 hangok telepítése**: Windows nem biztosít nyilvános, csendes
  telepítő API-t harmadik féltől; a program a hiányzó hangok észlelésekor
  megnyitja a Windows Beállítások megfelelő lapját (`ms-settings:speech`),
  ahonnan pár kattintással telepíthetők.
- A hangmagasság-eltolás (pitch bend) minden lenyomott hangnál egy-egy új
  megszólalást indít a kívánt hangmagassággal (SSML alapon) — ez ugyanaz a
  modell, mint amit a VocalWriter és a böngészős szótag-billentyűzetek is
  használnak; nem egy folyamatosan tartott hang élő hangmagasság-görbéje, mert
  egyik motor sem biztosít ilyen API-t.

## Build / futtatás (Windows szükséges)

Ez a projekt WinForms + WinRT (OneCore) interopot használ, ezért **csak
Windows alatt fordítható és futtatható** (Visual Studio 2022+ vagy a .NET 8
SDK Windows verziója szükséges).

```
dotnet build SpeechMidiKeyboard.sln -c Release
```

Portable, telepítő nélküli, egyetlen futtatható fájlba csomagolt kiadás:

```
dotnet publish src/SpeechMidiKeyboard/SpeechMidiKeyboard.csproj ^
  -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Az eredmény a `src/SpeechMidiKeyboard/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`
mappában lesz, ahonnan a teljes mappa másolható USB-kulcsra vagy bármely
Windows 10/11 gépre telepítés nélkül.

## Projektstruktúra

```
src/SpeechMidiKeyboard/
  Program.cs                 - belépési pont, indításkori motor-ellenőrzés
  MainForm.cs                - főablak, Alt-menü, puffer szerkesztő
  TextPromptForm.cs          - Ctrl+1 / Ctrl+2 beviteli párbeszédablak
  Keyboard/
    PianoKeyboardForm.cs     - Ctrl+K overlay: hangok, pitch bend, oktáv
    NoteKeyMap.cs            - billentyű -> hang/oktáv/bend-pont leképezés
  Services/
    IVoiceEngine.cs          - motor-független interfész
    Sapi5VoiceEngine.cs      - System.Speech (SAPI5) implementáció
    OneCoreVoiceEngine.cs    - Windows.Media.SpeechSynthesis (OneCore) implementáció
    Sapi4VoiceEngine.cs      - dokumentált, nem funkcionális SAPI4 stub
    VoiceEngineFactory.cs
    VoiceInstallChecker.cs   - motor/hang észlelés, Windows Beállítások megnyitása
    EngineCheckForm.cs       - indításkori figyelmeztető párbeszédablak
    PitchSnapshotStore.cs    - pitch_default.json kezelése
  Settings/
    AppSettings.cs           - settings.json (portable)
    SettingsForm.cs          - Beállítások ablak
```

## Állapot

Ez egy első, Linux alapú fejlesztői környezetben írt verzió — a WinForms/WinRT
kód emiatt még **nem lett Windows alatt lefordítva és tesztelve**. Kérlek
próbáld ki Visual Studióban vagy `dotnet build`-del Windows 10/11 gépen, és
jelezd a hibákat/eltéréseket a további finomításhoz.
