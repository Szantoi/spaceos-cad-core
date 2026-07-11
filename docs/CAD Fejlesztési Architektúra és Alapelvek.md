# **CAD Fejlesztési Architektúra és Alapelvek (Bútoripar fókusszal)**

Ez a dokumentum a professzionális CAD fejlesztés, különösen az AutoCAD, Inventor és SolidWorks platformokra épülő bútoripari gyártmánytervező rendszerek sarokköveit és legjobb gyakorlatait foglalja össze. Célja, hogy stabil, platformfüggetlen és tesztelhető alapokat nyújtson a fejlesztéshez.

## **1\. Architektúra: A 3-Rétegű Modell (Clean Architecture)**

A legfontosabb alapelv: **A CAD rendszer csupán egy megjelenítő (UI) és geometriai motor, nem az üzleti logika otthona.**

A rendszert három, szigorúan elválasztott rétegre kell bontani:

### **A) Core / Domain Réteg (A Párhuzamos Rendszer)**

* **Szerepe:** Itt található a teljes üzleti logika (bútorelemek definiálása, méretek, árazás, darabjegyzék generálás).  
* **Szabályok:** Tiszta C\# kód. **Szigorúan tilos** bármilyen CAD API referenciát (pl. acdbmgd.dll, SolidWorks COM) behivatkozni ebbe a projektbe.  
* **Azonosítás:** Objektumok azonosítása egyedi azonosítókkal (pl. Guid vagy string), sohasem CAD-specifikus azonosítókkal (pl. AutoCAD ObjectId).  
* **Tesztelés:** Mivel független a natív CAD függőségektől, MSTest vagy más tesztkeretrendszer segítségével 100%-ban és gyorsan Unit-tesztelhető.

### **B) Interfész Réteg (A Szerződés)**

* **Szerepe:** A Core rétegben definiált interfészek (pl. ICadEngine, IRenderer), amelyek meghatározzák, mit várunk el a CAD rendszertől (pl. RajzolSzekreny(), OlvasdKiMeretet()), de a megvalósítás részleteit nem.

### **C) Adapter / Infrastruktúra Réteg (A CAD Fekete Doboz)**

* **Szerepe:** Ez a projekt tartalmazza a CAD-specifikus API hivatkozásokat (AutoCAD SDK, Inventor API).  
* **Működés:** Ez az réteg valósítja meg az interfészeket. Lefordítja a Core Szekreny objektumát AutoCAD BlockReference \+ XRecord elemekké, és nyilvántartja a kapcsolatot a Core Guid és a CAD ObjectId között (pl. egy Dictionary segítségével).

## **2\. Adatkezelés az AutoCAD-ben (XRecord és Overrule)**

Az attribútumok (AttributeDefinition) elavultak és rugalmatlanok. A modern adatkezelés alapja a rejtett adattárolás.

### **Adattárolás: XRecord és Extension Dictionary**

* Minden AutoCAD objektum (vonal, polivonal, blokkreferencia) rendelkezhet egy rejtett szótárral (Extension Dictionary), amelyben XRecord-ok tárolhatók.  
* **Előnyök:** Bármikor bővíthető új mezőkkel a meglévő rajzokon (nem kell ATTSYNC), nincs méretkorlát (szemben az XData-val), és összetett adatstruktúrák tárolására is alkalmas.  
* **Biztonság:** Az adatok rejtve maradnak a felhasználó elől, megakadályozva a véletlen módosítást.

### **Adatmegjelenítés és Interakció (UI)**

* **Properties Overrule (OPM):** A .NET API (pl. IDynamicProperty) segítségével az XRecord adatok "beinjektálhatók" az AutoCAD gyári Tulajdonságok (Properties) paneljébe, mintha natív beállítások lennének (pl. "Lapanyag" választó egy vonalnál).  
* **Saját Palette (.NET / WPF):** Egy dedikált oldalsáv, amely teljes kontrollt ad az adatok megjelenítése és a műveletek (pl. CNC export) felett.  
* **Data Validation (Szűrés):** Az Overrule vagy Palette aktiválása előtt a kódnak ellenőriznie kell az objektum Extension Dictionary-jét egyedi "Marker" (pl. osztályazonosító) jelenlétére, hogy csak az intelligens bútoralkatrészeknél jelenjenek meg a gyártási adatok.

### **A Helyben Szerkesztés (REFEDIT) Veszélyei**

* A REFEDIT parancs a dinamikus blokkokat statikussá konvertálja, és gyakran megszakítja az XRecord kapcsolatokat (különösen, ha a szótár a blokk definícióján van).  
* **Védekezés:** Használj Event Handlert (CommandWillStart), amely figyeli a parancsokat, és ha egy intelligens blokkon a REFEDIT indulna, megszakítja azt, és a felhasználót a BEDIT (Blokkszerkesztő) vagy a saját Palette felé irányítja.

## **3\. Topológia és Geometria: A Térbeli Fizika Motor**

A bútoralkatrészek kényszerekkel történő (hagyományos CAD Mate/Flush) összekapcsolása nagy elemszámnál instabil (körkörös hivatkozások) és lassú. Ehelyett a professzionális játékmotorok logikáját kell alkalmazni a Core rétegben.

### **Affin Transzformáció és Helyi Tér**

* Minden alkatrész (pl. bútorlap) a saját **Helyi Térében** kerül definiálásra.  
* **A-Sík Szabály (CNC Sztenderd):** A lap jobb minőségű, színoldala (A-sík) a **Z=0** síkon fekszik. A lap vastagsága **negatív Z (-Z) irányban** növekszik.  
* **Miért fontos ez?** Így a megmunkálások (furatok, nútok) mélysége (Z koordinátája) független marad a lapvastagságtól (pl. egy 5 mm mély pánthely mindig Z=-5 helyen van, függetlenül attól, hogy 18 mm-es vagy 28 mm-es a lap).  
* A Globális Térbeli pozíciót egy **4x4-es Affin Transzformációs Mátrix** határozza meg (tartalmazza a forgatást, eltolást és léptékezést). A Z-eltolás (![][image1]) a lap színoldalának (Z=0) globális magasságát jelenti.

### **Bounding Volume Hierarchy (BVH) / AABB Optimalizálás**

* Minden alkatrész kap egy Tengelyhuzalos Határolódobozt (**AABB**), amely a helyi határok (![][image2]) és az Affin Mátrix szorzata alapján jön létre a globális térben.  
* **Optimalizálás:** Változások esetén a rendszer csak a globális dobozok metsződését vizsgálja, elkerülve a lassú, részletes geometriai számításokat.

### **A "Z-Gravitációs" Függőségi Modell**

* A gravitáció iránya a Z-tengellyel ellentétes (lefelé hat).  
* A függőségek kiszámítása (pl. egy szekrény a lábazaton támaszkodik) a Bounding Box-ok ütközésvizsgálatával történik.  
* Ha az alsó elem (A) magassága (Globális MaxZ) megnő és belelóg a felső elem (B) dobozába (Globális MinZ), a Z-gravitáció elve alapján a felső elem (B) Affin Mátrixának Z-eltolása (![][image1]) automatikusan megemelkedik a metsződés mértékével.  
* Ez a modell stabil, egyirányú (alulról felfelé) és mentes a CAD kényszerek okozta fagyásoktól.

## **4\. Iparági Praktikák és Fejlesztési Irányelvek**

* **Skeleton (Top-Down) Modellezés:** Inventor és SolidWorks környezetben a Master Part (Vezérfájl) alapú tervezés preferált. Az API-nak elég a Skeleton paramétereit módosítania, a származtatott alkatrészek (Derive Components) frissítését és újraépítését a CAD rendszer natív, robusztus matematikai motorja végzi.  
* **Memóriakezelés:** Unmanaged CAD erőforrások (pl. COM objektumok, Transaction-ok) szigorú felszabadítása (using, Marshal.ReleaseComObject), a memóriaszivárgások és fagyások elkerülése érdekében.  
* **Batch (Tömeges) Feldolgozás:** Minimalizáld a CAD API hívások számát. Olvass be/írj ki mindent egyszerre, végezd a számításokat a Core rétegben, így elkerülhetők a lassú for ciklusokon belüli egyedi API interakciók.  
* **Atomáris Tranzakciók:** Biztosíts Rollback (visszagörgetési) mechanizmust hibák esetére, hogy a rajz ne maradjon inkonzisztens állapotban egy megszakadt generálási folyamat után.

[image1]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABMAAAAZCAYAAADTyxWqAAABUklEQVR4XmNgGAVkAXl5+RNA/B+IvwDxLCheDxW7AuIrKCgsBNIPgPirnJycMboZcABU8A2ouBLIZEYSCwLi30CNNkhKGYFiT6SlpWWQxFABUMEqY2NjVjSxSUB8V1FRURxN/LC6ujovshgcACUtlZSU+NHENIH4LRAXIYtD5arQxeAA6A1XdDGoF/+jeREMZGVlTdHF8AKoF/+je5FkgOTF/+hyJAOgIdEgg4D4N7ocyQDmRSC+ii5HMoB5EZju0tHlSAYwL2KLSRCQkZERAsovB+JvQC4z0NICINsTrgDIkYRiJ6hhV4AxqQcSAyrmQBjFwABNsIwgNlCuEyWxi4qK8kANwIZxuhAo54Sea8gBjEALQoC4HF2CZAD02i2gqyYDDfMF0vOA/EYGqLdJAsDAVwGWGMLALOUnDymK5ouLi3OjqyMKIEcGKKyR5UYBeQAAe49ZE1PlAVYAAAAASUVORK5CYII=>

[image2]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEEAAAAZCAYAAABuKkPfAAADA0lEQVR4Xu1XO2gUURTdIREUBfGzLu5v9tMY0GohQhC0EDQERRKLiIJiI1gmoJDKVrAKFiGdVUDUShBxkWhlGxAUIWghBBRdUliImnhO9j65ubuzzuxMscUcuMy8+znvvrv3vTebyaRIkSJFMIZ9378MeQLZhKxXKpW7zlgsFo+JnvIeMg7dLk0QBPCcgky7eLx/IJ/2aTQaO2CbFJ8v5XL5XFh+DcQUEP9JeJbBcwtykYJ5L4m+Va1WR23sP8CYg9M7Ct+1DbrvIDqDV0/rwwLxa5LEG2sjCoXCASS70M/iHUql0nmZo6n1LDJyvwP9D8iEtnWFOPMXm3Y6jMd6Vi8EwLEkCf60NumERT6tLQrAMY+8X9dqtb1K7bEjZO4ppe8Jbg0GsG3w8FdAfM06RYVuVZ2kJMjO87V/PwDPS1MA6lqynnta/19IEGWFSWb63AIGHvjukxecJ5wS47W4XeaAH+u4HpNX1hG9y/z2ubBVvcjBPYAkz4JzAzLPsSQZukWjgJ3FQxj8TdsdocDKSRGWMBy29jhgAaQQ69JliYOLZv4sgrWFBQ+SV5DnIGrhxD1qHeIAvKelwNFbNATcIcvczTbzwt483LezbCV33SDp29YpDqQIv/S5kBTUVcgiT2obxiM8nLWuA3CagHzU1ROyDe5l7RsDvHkeUfhujXEB3j/yw23bZtlsdg/0y1rXFXD63KV67nrh2RAIFGmn1XWDuiZnrK0HuD334TlkDRrgHJNcO7YZ4q9A/1XrHIbYmljAA1aQ7a8XI3vrsRBvcovU6/VDjFMcnHxGfJq5XG63tjmQS745HtIX85zE83CY4sFv1eVgbYS6Bch7hLzCfQHPmy4W4xs21l1XWw5Kxp0d79e72Fer5nNaFveWiVibQwDXtq/SIMBv1m/fJr+tjfDbt00Ht5FvkBEbmzgwybN8Pn/Q6pMC+OesbtDghflV+wWvaRThqdUPFJDgFP8FWn0SkA+fFzzcrG1ggBN/Pz+srD4poMOugn/BnvgpUqRIkTT+Ah6M/WqWq7LSAAAAAElFTkSuQmCC>