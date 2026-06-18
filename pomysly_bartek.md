# Pomysły Bartek
## Gameplay

### Wersja dynamiczna
Albo robimy żeby rozgrywka była bardziej dynamiczna, taki klasyczny arcade - szybkie respawny, szybkie powerupy, areny mniejsze (ale i tak większe niż to co jest), ale z jakimiś rampami itd. Myślę, że standardowo mecze po 5 minut.

### Wersja "strategiczna"
Albo robimy żeby rozgrywka była wolniejsza, ale bardziej strategiczna - wolniejsze respawny, powerupy rzadszy reps, większe zróżnicowanie, ale o wiele bardziej wpływające na rozgrywkę. Np powerupy można też zebrać
po zabiciu jakiegoś gracza (i potwierdzić klawiszem, czy zastąpić powerupa. Na pewno wtedy tryb drużynowy potrzebny.

No i może jakieś lvl'owanie wtedy czy tam "stałe" ulepszenia na czas rozgrywki. Tylko coś takiego myślę, że mecze po 10-30 minut. W Tron Evolution pamiętam, że mecze tyle trwały, nawet spoko się tak grało, bo areny był spore
i system jeżdzenia był zajebisty, bo można było w trakcie jazdy zmieniać sie w ludzika i napierdalać tymi talerzami z Trona w innych i system walki był dojebany i w każdym momencie można było zmienić się spowrotem na pojazd. 
No tutaj nie mamy systemu, żeby sterować postacią, to trzeba by było nadrobić właśnie innymi rzeczami.

### ????
Osobiście bym poszedł w wersję dynamiczną, można część elementów dać do wersji dynamicznej z strategicznej i tak.

## Areny
Zależy w którą wersję pójdziemy gameplay, ale luźne pomysły:

### Zrobienie kategorii "open aren"
Czyli dużo otwartego miejsca, mało przeszkód, coś jak mamy teraz, ale nie aż tak basicowe.

### Zrobienie kategorii "closed aren"
Czyli podzielenie na jakieś bardziej żróznicowane obszary, w miare wąskie przejścia do innych obszarów itp.

### Zrobienie kategorii "labs aren"
Jakieś np. podzielone na strefy? Strefa gdzie się dostaje co chwila powerupa/strefa gdzie jest wszystko przyspieszone/zwolnione/strefa gdzie jak byłby tryb drużynowy to trafiasz do innego teamu
i jak kogoś zabijesz to robisz gorzej dla swojej drużyny, bo to drużyna przeciwna dostaje punkt XD - i np. strefa taka bęedzie jedyną drogą do strefy w której zabicia są liczone 2/3x bardziej. Może być ciekawe xD.

### Urozmaicenia
- przeszkody
- rampy
- tunele/mosty
- powerupy w ryzykownych miejscach

## Powerupy
- Więcej rodzaji power'upów (i tu znowu zależnie od której wersji, bo w wersji strategicznej trzeba by było poprzekminiać dobrze konkretne pomysły)
- Możliwość odpalenia ich przez gracza/bota, a nie, że od razu.
### Luźne pomysły na pozytywne/negatywne powerupy:
- Chudszy i grubszy trail
- Krótszy i dłuższy zostający trail
- Brak generowania traila na jakiś czas
- Przenikalność
- Boosty/Spowolnienia
- Ukradnięcie powerupa
- Jednarazowa tarcza

## UI
### UI Toolkit
Przenosimy UI sceny do UI Toolkit - UXML + USS + C# Controllers (zamiast tradycyjnych scen z skomplikowaną hierarchią mamy system jak HTML/XML + CSS). Dzięki temu mamy łatwą generację przez AI, większość wtedy to sam kod.

### Notification/Toast System
- Notifikacje podczas np. w prawym górnym rogu:
  - kto kogo zabił.
  - kto jakiego powerupa wykorzystał.
  
### Luźne pomysły
- Boty jeżdzące sobie same w menu w tle po tym planie.

#----------------------------
## Feedback chata
# Feedback do propozycji gameplayu THRONE

## Ogólne podsumowanie

Najbardziej sensownym kierunkiem dla THRONE wydaje się wersja dynamiczna. Obecny rdzeń gry — pojazdy, traile, szybkie eliminacje, respawny, punkty i arena — naturalnie pasuje do arcade’owego tempa. Wersja strategiczna też ma potencjał, ale wygląda bardziej jak większy, późniejszy tryb niż podstawowy kierunek gry.

Największe ryzyko jest takie, że projekt zacznie próbować być jednocześnie szybką grą arcade, długim strategicznym arena combatem, eksperymentalnym trybem z mutatorami i grą drużynową. Wszystkie te pomysły mogą działać, ale nie powinny być rozwijane naraz.

Najlepszy kompromis: zrobić dynamiczną wersję jako bazę, ale część systemów projektować tak, żeby później można było dołożyć bardziej strategiczne elementy.

---

## Gameplay

### Wersja dynamiczna

To według mnie najmocniejszy kierunek na teraz. Krótkie mecze, szybkie respawny, częste powerupy i bardziej dopracowane areny dobrze pasują do tego, czym THRONE już jest. Taki model jest też łatwiejszy do testowania, balansowania i doprowadzenia do grywalnego stanu.

Mecze po około 5 minut mają sens. Taki czas jest wystarczający, żeby mecz miał przebieg i wynik, ale nie jest tak długi, żeby pojedyncze problemy z balansem albo śmierć gracza za bardzo frustrowały. Przy dynamicznej rozgrywce gracz powinien szybko wracać do akcji, a nie czekać długo po każdej eliminacji.

Ten kierunek wymaga przede wszystkim dobrego „game feel”: responsywnego sterowania, czytelnych kolizji, szybkiego feedbacku po zabiciu, jasnego UI i powerupów, które faktycznie zmieniają sytuację w meczu. Nie musi być od razu bardzo dużo mechanik. Lepiej mieć kilka dobrze działających elementów niż wiele niedokończonych.

### Wersja strategiczna

Wersja strategiczna brzmi ciekawie, ale jest znacznie bardziej wymagająca. Dłuższe mecze 10–30 minut mają sens tylko wtedy, kiedy gracz ma więcej decyzji niż samo jeżdżenie, unikanie traili i zbieranie powerupów. W Tron Evolution działało to dlatego, że była transformacja między pojazdem a postacią, większa mapa, walka wręcz/dyskami i więcej sposobów grania.

W THRONE, bez systemu postaci, trzeba by tę głębię zbudować inaczej: przez team mode, mocniejsze powerupy, kontrolę stref, ulepszenia w trakcie meczu, dropy po zabiciu i większe mapy. To są dobre pomysły, ale razem tworzą już dużo większy scope.

Dlatego nie traktowałbym wersji strategicznej jako podstawowego trybu na teraz. Bardziej jako inspirację dla przyszłych systemów albo osobny tryb, który może powstać dopiero wtedy, gdy dynamiczna wersja będzie już solidna.

### Najlepszy kierunek

Najlepszym wyborem wydaje się dynamiczna wersja z wybranymi elementami strategicznymi. Czyli gra nadal powinna być szybka, arcade’owa i czytelna, ale powerupy oraz areny mogą dawać graczowi trochę decyzji taktycznych.

Dobrym przykładem jest system powerupów aktywowanych przez gracza. To nadal pasuje do arcade, ale dodaje decyzję: kiedy użyć boosta, kiedy odpalić tarczę, kiedy przejechać bez traila itd. To daje głębię bez zmieniania gry w wolniejszy strategiczny tryb.

---

## Areny

### Open Arena

Open arena powinna być podstawowym typem mapy. Taka arena najlepiej pokazuje główny gameplay: jazdę, trail, pozycjonowanie, powerupy i eliminacje. Powinna mieć dużo przestrzeni, ale nie może być zbyt pusta, bo wtedy gameplay szybko stanie się monotonny.

Dobrze byłoby zachować otwartość, ale dodać kilka charakterystycznych elementów: rampy, przeszkody, ryzykowne powerupy, może jeden centralny obszar walki i kilka bocznych tras. To nadal byłaby prosta arena, ale mniej „basicowa”.

### Closed Arena

Closed arena jest dobrym pomysłem jako drugi typ mapy. Wąskie przejścia, podział na sekcje i tunele mogą bardzo dobrze współgrać z trailami, bo wtedy blokowanie drogi i przewidywanie ruchu przeciwnika staje się ważniejsze.

Trzeba tylko uważać, żeby taka mapa nie była zbyt frustrująca. Jeżeli korytarze będą za wąskie albo respawny źle ustawione, gracz może ginąć od razu po odrodzeniu albo czuć, że nie ma przestrzeni do reakcji. Closed arena powinna być bardziej techniczna, ale nadal płynna.

### Labs Arena

Labs arena to bardzo dobry kierunek dla eksperymentalnych map. Strefy z różnymi efektami mogą dać grze charakter i odróżnić ją od zwykłego Tron-clone. To jest też dobre miejsce na bardziej szalone pomysły, których niekoniecznie chcesz mieć w podstawowym trybie.

Strefa zmiany drużyny jest śmieszna i może być ciekawa, ale raczej jako eksperymentalny gimmick niż główna mechanika. Taki efekt musi być ekstremalnie czytelny, bo inaczej gracz nie będzie rozumiał, dlaczego punkt poszedł do innej drużyny. Musiałoby być mocne oznaczenie kolorem, komunikat UI i jasna informacja, że gracz jest tymczasowo po innej stronie.

Bardziej kontrolowana wersja tego pomysłu mogłaby działać jako „risk zone”: wjeżdżasz w niebezpieczną strefę, gdzie możesz zdobyć większą nagrodę, ale zabójstwa albo punkty działają inaczej. To pasuje do labs arena, bo tam gracz spodziewa się dziwnych zasad.

### Urozmaicenia

Przeszkody, rampy, tunele, mosty i powerupy w ryzykownych miejscach są dobrymi pomysłami. Najważniejsze, żeby nie były tylko dekoracją. Każdy element areny powinien wpływać na decyzje gracza: czy ryzykować przejazd, czy odciąć komuś drogę, czy zgarnąć powerup, czy uciec inną trasą.

---

## Powerupy

Powerupy są prawdopodobnie najlepszym miejscem na dodanie głębi bez zmieniania całego charakteru gry. Najważniejsza decyzja: powerupy powinny być aktywowane przez gracza, a nie odpalać się automatycznie po podniesieniu. To od razu robi gameplay ciekawszy, bo gracz musi wybrać moment użycia.

Na start wystarczyłby jeden slot powerupa. Gracz podnosi powerup, widzi go w HUD i odpala przyciskiem. Dopiero później można dodać podmianę, potwierdzanie wyboru, kradzież powerupa albo drop po zabiciu.

Najlepsze powerupy na początek to takie, które są proste do zrozumienia i od razu widoczne w grze: boost, spowolnienie, tarcza, brak traila przez chwilę, przenikalność, krótszy/dłuższy trail, cieńszy/grubszy trail. To są efekty, które gracz szybko zrozumie bez tutoriala.

Bardziej złożone pomysły, jak kradzież powerupa, drop po zabiciu albo stałe ulepszenia w trakcie meczu, zostawiłbym na później. One są ciekawe, ale wymagają lepszego balansu, UI i logiki botów.

---

## UI

### UI Toolkit

Przenoszenie UI do UI Toolkit ma sens, szczególnie dla menu, lobby, ustawień i game over screen. To pasuje do Twojego workflow, bo UXML + USS + C# controllers są łatwiejsze do generowania i refaktorowania przez AI niż skomplikowana scena z ręcznie ustawianą hierarchią obiektów.

Nie przenosiłbym jednak wszystkiego naraz. Najbezpieczniej zacząć od ekranów menu i lobby, bo tam UI Toolkit daje największy porządek, a ryzyko rozwalenia gameplayu jest mniejsze. HUD w trakcie meczu można przenieść później, kiedy system powerupów, toastów i score display będzie już lepiej określony.

### Notification / Toast System

Toast system bardzo pasuje do THRONE. Gra potrzebuje szybkiego feedbacku: kto kogo zabił, kto użył powerupa, kto zdobył punkt, kiedy zaczyna się sudden death, kiedy aktywuje się specjalna strefa itd.

Toasty w prawym górnym rogu byłyby dobrym rozwiązaniem. Powinny być krótkie, czytelne i nie zasłaniać akcji. Limit kilku widocznych naraz ma sens, żeby UI nie zrobiło się chaotyczne.

Ten system może też bardzo pomóc przy bardziej eksperymentalnych mechanikach. Jeżeli gracz wjedzie w strefę zmiany drużyny albo double score zone, toast może natychmiast wyjaśnić, co się stało.

### Boty w menu

Boty jeżdżące w tle menu to fajny pomysł na klimat, ale traktowałbym to jako polish feature. Może bardzo dobrze wyglądać, szczególnie z neonowym stylem, trailami i wolno poruszającą się kamerą, ale nie powinno blokować ważniejszych rzeczy.

Najpierw gameplay, powerupy, areny i podstawowe UI. Dopiero później menu z botami w tle.

---

## Wniosek

Najlepszy kierunek dla THRONE to dynamiczna arcade’owa wersja jako główny tryb. Strategiczne elementy są dobre, ale powinny być dokładane selektywnie, a nie jako fundament od razu.

Na teraz najważniejsze wydają się: dopracować krótki deathmatch, zrobić aktywowane powerupy, poprawić areny, dodać czytelny feedback UI i dopiero potem eksperymentować z team mode, labs arenas, dropami po zabiciu i dłuższymi meczami.

THRONE powinno najpierw być szybką, czytelną i przyjemną grą arcade. Dopiero potem może stać się bardziej strategiczne.
