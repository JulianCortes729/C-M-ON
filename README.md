# C-M-ON 🕷️🏙️

> **Proyecto Final - Curso Unity 3D (Talento Tech)**
>
> *Un plataformero 3D de acción y precisión.*

[![Jugar en Unity Play](https://img.shields.io/badge/Jugar%20Online-Unity%20Play-222c37?style=for-the-badge&logo=unity)](https://play.unity.com/en/games/c9c5e95f-bb60-4f34-816d-dd38df96bf3d/c-m-on)

![Unity](https://img.shields.io/badge/Unity-2022.x-black?style=flat&logo=unity)
![Talento Tech](https://img.shields.io/badge/Curso-Talento%20Tech-orange)
![Status](https://img.shields.io/badge/Status-Demo%20Finalizada-success)

![Vista del proyecto en Unity Editor](Captura%20de%20pantalla%202024-11-02%20193303.png)

## 📖 Sobre el Proyecto

**C-M-ON** fue desarrollado como la **entrega final** para el curso de **Unity 3D de Talento Tech**.

Es un videojuego de plataformas en 3D donde el objetivo es liberar una ciudad futurista infestada de enemigos. En esta demo técnica, el jugador debe escalar un edificio lleno de trampas mortales, plataformas inestables y enemigos arácnidos, culminando en una intensa batalla contra un **Jefe Final (Boss)** en la azotea.

> 🌐 **[¡Prueba la demo directamente en tu navegador aquí!](https://play.unity.com/en/games/c9c5e95f-bb60-4f34-816d-dd38df96bf3d/c-m-on)**

## 🎮 Mecánicas Implementadas

El juego demuestra la implementación de físicas, controladores de personajes y lógica de juego avanzada:

* **Movilidad (Player Controller):**
    * **Doble Salto:** Lógica para control aéreo y corrección de trayectoria.
    * **Dash:** Impulso horizontal para esquivar y alcanzar plataformas lejanas.
* **Sistema de Combate:**
    * **Disparo:** Raycast/Proyectiles para dañar al Boss.
    * **Aplastamiento:** Detección de colisiones verticales para eliminar arañas.
* **Entorno e Interacción:**
    * **Trampas:** Pinchos, plataformas eléctricas y suelos destructibles.
    * **Checkpoints:** Sistema de persistencia de posición tras morir.

## 👾 Inteligencia Artificial (IA)

* **Arañas:**
    * Sistema de patrullaje (Waypoints).
    * Detección de jugador (Rango de visión) y persecución (NavMesh).
    * *Mecánica especial:* Transformación de items (monedas) en explosivos.
* **Boss Final:**
    * Máquina de estados con patrones de ataque (disparos y aplastamiento).
    * Gestión de eventos de animación.

## 🕹️ Controles

| Acción                   | Tecla / Input     |
| :------------------------|:----------------- |
| **Moverse**              | WASD              |
| **Saltar / Doble Salto** | Barra Espaciadora |
| **Dash**                 | C                 |
| **Disparar**             | Click Izquierdo   |
| **Sensibilidad**         | TAB               |
| **Saltar introducción**  | Q                 |

## 👥 Créditos y Autoría

Este proyecto es una demostración técnica desarrollada individualmente para fines académicos.

* **Desarrollo y Programación:** **Julián Cortés**
    * Lógica completa del juego (C#), diseño de niveles, implementación de IA, UI y mecánicas.

* **Assets de Terceros:**
    * **Arte y Sonido:** Los modelos 3D, texturas, música y efectos de sonido pertenecen a sus respectivos autores. Se han utilizado y/o modificado únicamente con fines educativos para dar vida a las mecánicas programadas.

---
*Desarrollado para Talento Tech - 2025*
