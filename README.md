# Cocorico 2.0 - Application AR d'Éducation Nutritionnelle

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-black?logo=unity)
![AR Foundation](https://img.shields.io/badge/AR%20Foundation-5.1-blue)
![Android](https://img.shields.io/badge/Android-7.0%2B-green?logo=android)
![Status](https://img.shields.io/badge/Status-Beta%20v1.0-orange)

**Cocorico 2.0** est une application mobile de réalité augmentée qui transforme l'éducation nutritionnelle en une expérience interactive et immersive. L'utilisateur pointe sa caméra vers des aliments ou des fiches imprimées et reçoit instantanément des informations nutritionnelles affichées en AR.

[📥 Télécharger l'APK](../../releases/latest) · [📄 Fiche Pédagogique PDF](../../releases/latest)

</div>

---

## Origine du projet

Cocorico 2.0 est l'évolution mobile et AR d'un projet académique antérieur - **Cocorico** - une application web de gestion alimentaire intelligente développée à l'ESPRIT. La partie garde-manger virtuel et détection d'ingrédients par IA, initialement développée en microservices Python (YOLOv8, Flask), est ici réimagée en expérience de réalité augmentée sur mobile Android.

---

## Modes disponibles

### Mode actif - Fiche Pédagogique (Image Tracking)

Le seul mode entièrement fonctionnel dans cette version beta.

**Comment l'utiliser :**
1. Télécharge ou imprime la [Fiche Pédagogique PDF](../../releases/latest)
2. Lance l'application sur ton téléphone Android
3. Sur l'écran de démarrage, attends le chargement (3 secondes)
4. Clique sur **"Fiche Pédagogique"** dans le menu principal
5. Pointe la caméra vers l'une des 6 images de la fiche
6. Une fiche nutritionnelle AR apparaît au-dessus de l'image
7. Consulte les infos (calories, Nutri-Score, additifs, conservation)
8. Clique **"Faire le quiz"** pour tester tes connaissances
9. Compare ton score avant / après sur la fiche papier

---

### Modes en développement

| Mode | Description | Statut |
|------|-------------|--------|
| Fruits & Légumes | Détection temps réel via YOLOv8 + Flask | En cours |
| Code-barres | Scan EAN + Open Food Facts API | En cours |

---

## Installation

### Prérequis
- Android 7.0+ (API 24)
- ARCore compatible ([liste des appareils](https://developers.google.com/ar/devices))
- Activer **Sources inconnues** dans les paramètres de sécurité

### Installation de l'APK
1. Télécharge `Cocorico2-beta.apk` depuis les [Releases](../../releases/latest)
2. Ouvre le fichier sur ton téléphone Android
3. Accepte l'installation depuis une source inconnue
4. Lance l'application

---

## Stack technique

```
Mobile AR
├── Unity 2022.3.62f3 LTS
├── AR Foundation 5.1 (ARCore backend)
└── C# Scripts

Intelligence Artificielle (Mode 1 - en développement)
├── YOLOv8 (détection fruits & légumes)
├── Flask / Python (serveur local)
└── Communication HTTP JSON

API Externe (Mode 2 - en développement)
└── Open Food Facts (gratuit, sans clé)

Assets 3D
└── Blender 3.x (modèles GLB - panneaux AR)
```

---

## Structure du projet Unity

```
Assets/
├── Models/          # Modèles 3D Blender (.glb)
├── Prefabs/         # PedaCard_Prefab, TestCube
├── Resources/       # peda_content.json
├── Scenes/          # SplashScene, MainScene
├── Scripts/         # C# scripts
│   ├── ModeManager.cs
│   ├── PedaCardController.cs
│   ├── PrefabImagePairManager.cs
│   ├── PedaContentData.cs
│   └── SplashController.cs
└── UI/
    └── TargetImages/  # Images cibles AR
```

---

## Auteur

**Hend Zormati**
Double diplôme Ingénierie - ESPRIT × ENSIM
[Portfolio](https://hendzormati.github.io/Portfolio)

---

## 📄 Licence

Projet académique - tous droits réservés.