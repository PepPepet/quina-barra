# Quina Barra!

*El temps del teu poble a la barra de tasques, en català.*

<sub>El temps del teu poble a la barra de tasques, en valencià · O tempo da túa vila na barra de tarefas, en galego · Zure herriko eguraldia ataza-barran, euskaraz · El tiempu del to pueblu na barra de xeres, n'asturianu · O tiempo d'o tuyo lugar en a barra de fayenas, en aragonés · Lo temps de ton vilatge dins la barra de prètzfaches, en occitan · Eth temps deth tòn pòble ena barra de prètzhèts, en aranés · Lo temps de la tua vila a la barra de tasques, en alguerés.</sub>

Giny per a Windows 11 que posa **el temps del teu poble** a l'esquerra de la barra de tasques, on hi havia el giny del temps de Hasefroch (sí, aquell que de vegades et parla en àrab), però **en català** i sense notícies ni anuncis.

![El giny a la barra de tasques](docs/captures/barra.png)

**[⬇ Baixa la darrera versió](https://github.com/PepPepet/quina-barra/releases/latest)** · un sol fitxer, `QuinaBarra.exe`, sense instal·lador.

<p>
  <img src="docs/captures/panell-fosc.png" width="380" alt="Panell en mode fosc">
  <img src="docs/captures/panell-clar.png" width="380" alt="Panell en mode clar">
</p>

## Què fa

- **Temps d'ara a la barra**: icona, temperatura i estat del cel, sempre visibles.
- **Tu tries els llocs** (pobles, ciutats o barris) i en pots desar uns quants. No es fa servir mai la IP, que sovint diu que ets a una altra banda.
- **Temperatura més fiable**: mitjana de tres models (Météo-France, ICON i ECMWF).
- **Què ens espera**: un resum en una frase de la pluja de les pròximes hores i de la tendència de les temperatures.
- **Pròximes 24 hores** i **pròxims 10 dies**.
- **Rajoles** de precipitació, vent, sortida i posta del sol, índex UV, qualitat de l'aire (escala oficial ICA), visibilitat, humitat i pressió. Cada rajola obre una **pàgina de detall** amb gràfic per hores i explicació.
- **Mar**, als llocs de costa: onades, període, mar de fons, temperatura de l'aigua i l'estat de les platges (bandera, meduses, transparència).
- **Radar de pluja** de les últimes 2 hores, animat, sobre un mapa d'OpenStreetMap.
- **Configuració** (roda dentada del menú ⋯): la llengua i **Personalitza el panell**, per triar quins blocs vols veure i en quin ordre. El que amaguis ja no es baixa d'internet.
- Als municipis de Catalunya, enllaç directe a la previsió de **Meteocat**.
- **En nou llengües**: català, valencià, gallec, basc, asturià, aragonès, occità, aranès i alguerès. Agafa la del Windows si la té, i la pots canviar a **Configuració**.
- Mode clar, fosc o com el Windows. S'amaga quan mires un vídeo a pantalla completa.
- Lleuger: uns 100 MB de memòria i pràcticament gens de processador.

<p>
  <img src="docs/captures/detall-vent.png" width="250" alt="Detall del vent">
  <img src="docs/captures/detall-sol.png" width="250" alt="Sortida i posta del sol">
  <img src="docs/captures/personalitza.png" width="250" alt="Configuració: llengua i blocs del panell">
</p>

## Com s'instal·la

1. Baixa `QuinaBarra.exe` de la [pàgina de la darrera versió](https://github.com/PepPepet/quina-barra/releases/latest) i desa'l on vulguis (per exemple, a `C:\Users\<el teu usuari>\QuinaBarra`).
2. Fes-hi doble clic. Com que el programa no té una signatura digital de pagament, Windows pot mostrar **«Windows ha protegit l'ordinador»**: prem **Més informació** → **Executa igualment**.
3. El primer cop et demanarà el teu lloc.
4. Perquè s'engegui sol: menú **⋯** → **Quant a** → marca **Engega amb el Windows**.

Un cop al dia mira si hi ha una versió nova. No la baixa ni la instal·la pel seu compte: t'avisa i obre la pàgina de GitHub de la versió nova.

## Com es fa servir

- **Clic** al giny: obre el panell. **Clic** a una rajola: n'obre el detall. Per tornar: la fletxa ←, la tecla Esc o el botó lateral «enrere» del ratolí.
- **Pastilles de llocs**: un clic hi salta; **arrossega-les** per canviar-ne l'ordre (la primera és la principal); **+ Afegeix** en desa un de nou; amb el **botó dret** el pots fer principal, canviar-li el nom, moure'l o treure'l. El principal (★) és el que surt a la barra.
- **Endreça el panell** (a sota de tot): arrossega les targetes per canviar-ne l'ordre i prem **Fet**.
- **Menú ⋯**: Configuració (llengua i blocs del panell), Quant a i Tanca el giny.

## Llengües

| Llengua | Fitxer |
|---|---|
| Català (original) | `src/textos/ca.json` |
| Valencià | `src/textos/ca-valencia.json` |
| Gallec | `src/textos/gl.json` |
| Basc | `src/textos/eu.json` |
| Asturià | `src/textos/ast.json` |
| Aragonès | `src/textos/an.json` |
| Occità | `src/textos/oc.json` |
| Aranès | `src/textos/oc-aranes.json` |
| Alguerès | `src/textos/ca-alguer.json` |

<p>
  <img src="docs/captures/panell-gl.png" width="380" alt="Panell en gallec">
  <img src="docs/captures/panell-eu.png" width="380" alt="Panell en basc">
</p>

Som de Catalunya i el giny l'hem fet en català. Les altres llengües les hem traduït amb l'ajuda de Claude, la IA d'Anthropic, i les hem revisat amb cura, però no som lingüistes: si hi trobes errors, els suggeriments són benvinguts. El giny tria la llengua del Windows si la té (el Windows té català, valencià, gallec i basc) i, si no, la pots triar a mà a **Configuració**. Cada llengua és un fitxer de text al format de jquery.i18n; `src/textos/qqq.json` explica on surt cada text.

## On desa les coses i com es desinstal·la

La configuració és a `%APPDATA%\QuinaBarra\` (`config.json`, amb els llocs, el mode i la llengua; una còpia `config.bak` i un registre petit `registre.log`).

Per desinstal·lar-lo: desmarca **Engega amb el Windows**, tria **Tanca el giny** i esborra `QuinaBarra.exe` i la carpeta `%APPDATA%\QuinaBarra`.

## Seguretat

`QuinaBarra.exe` no té una signatura digital de pagament, i per això el Windows avisa el primer cop que l'obres. Per comprovar que és el de debò:

- L'**empremta SHA-256** de cada versió és a les notes de la seva [release](https://github.com/PepPepet/quina-barra/releases).
- **Anàlisi de la 1.1 a [VirusTotal](https://www.virustotal.com/gui/file/c51d4f56b7937ab080f3da46e9248dbe2c86c95395b9a9d639edd68a91a507eb)**: 2 de 70 antivirus el marquen per detecció automàtica d'«anomalies» (Trapmine i SecureAge). És un fals positiu habitual en programes petits i nous sense signatura de pagament: els hem avisat perquè el revisin, i Malwarebytes, que també el marcava, ja l'ha corregit després del nostre avís. Els principals (Microsoft Defender, Kaspersky, ESET, Bitdefender, Avast, Norton, McAfee, Malwarebytes...) no hi troben res.
- El **codi és obert**: el pots llegir i compilar tu mateix (vegeu més avall).

## Privadesa

No recull cap dada teva. Només envia les coordenades dels llocs que has desat als serveis del temps per demanar-ne la previsió. La configuració es queda al teu ordinador.

## D'on surten les dades

- Previsió, qualitat de l'aire i mar: [Open-Meteo](https://open-meteo.com/) (CC BY 4.0), amb models de Météo-France, DWD (ICON), ECMWF i Copernicus (CAMS). El cercador de llocs és el geocodificador d'Open-Meteo (basat en GeoNames).
- Qualitat de l'aire classificada amb l'escala oficial **ICA** del [MITECO](https://www.miteco.gob.es/ca/calidad-y-evaluacion-ambiental/temas/atmosfera-y-calidad-del-aire/visualizacion-datos-calidad-del-aire/ica.html): partícules en mitjana de 24 h, ozó de 8 h, NO₂ i SO₂ de la darrera hora.
- Noms i codis oficials dels municipis de Catalunya: [Idescat](https://www.idescat.cat/).
- Estat de les platges: [dades obertes de la Generalitat](https://analisi.transparenciacatalunya.cat/d/4baz-cjv2) (només en temporada de bany; fora de temporada es mostra l'últim estat amb la data).
- Radar: [RainViewer](https://www.rainviewer.com/).
- Mapa: © [col·laboradors d'OpenStreetMap](https://www.openstreetmap.org/copyright). Les rajoles es desen al disc 30 dies per no sobrecarregar els seus servidors.

## Compilar-lo tu mateix

El codi font és a `src\` (C#, .NET Framework 4.8, que ja porta el Windows). No cal instal·lar res:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File src\build.ps1
```

L'`.exe` surt a `exe\QuinaBarra.exe`. Amb `QuinaBarra.exe /prova <carpeta> [llengua]` en fa captures de totes les pantalles sense tocar la configuració (per exemple, `/prova C:\captures gl` les fa en gallec) i avisa si falta algun text.

Els suggeriments i els errors, a les [incidències](https://github.com/PepPepet/quina-barra/issues), en català.

## Autoria i llicència

Fet per [Pep Payo](https://peppayo.com) amb l'ajuda de Claude, la IA d'Anthropic.

Codi obert amb **llicència MIT** (vegeu el fitxer [LICENSE](LICENSE)): el pots fer servir, copiar, modificar i compartir lliurement, sempre que hi mantinguis l'avís d'autoria. Es dona tal qual, sense cap garantia.
