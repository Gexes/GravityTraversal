# GravityTraversal

A intermediate-grade Unity 6 framework for planetary traversal featuring dynamic gravity, spherical world movement, surface-aligned character orientation, custom camera systems, and launch mechanics for seamless navigation between planets.

## Features

- **Dynamic Multi-Shape Gravity:** Custom gravitational equations supporting Spherical cores, Axis-Aligned Box projections, and Capsule line-segments.
- **Pure Screen-Space Input Mapping:** Advanced vector translation that completely eliminates Yaw Inversion and directional confusion when traveling upside down.
- **Immune to Gimbal Lock:** Utilizes absolute Quaternion-based angular offsets to entirely bypass polar singularity locks and infinite circle glitches.
- **Cinemachine 3.x Native Integration:** Custom camera target tracking configured specifically for *Orbital Follow* and *Lazy Follow* paradigms.
- **Zero-Jitter Physics Loop:** Built on Rigidbody forces synchronized strictly to the Unity 6 `FixedUpdate` physics engine clock.
- **Spline-Driven Launch System:** Seamless inter-planetary transit running along native Unity Splines with dynamic mid-air orientation alignment.
- **Editor Placement Tools:** Specialized `ExecuteAlways` utility components to easily decorate and align level geometry in real-time.

## Built With

- **Unity 6** (6000.0+ LTS)
- **C#** Core Scripting
- **Unity Input System** (Refactored input buffers)
- **Cinemachine 3.1.7** (Position Control: Orbital Follow / Binding Mode: Lazy Follow)
- **Animancer** (State-driven skeletal animation management)
- **DOTween** (Demigiant Tweener Engine for environmental visuals)
- **Unity Splines Package** (Parametric track generation)

## Overview

GravityTraversal is a high-utility technical gameplay prototype focused on recreating the signature planetary movement mechanics commonly found in gravity-based platforming games like *Super Mario Galaxy*.

The project isolates core mathematical and physics limitations—such as coordinate flattening over steep curves, viewport tracking conflicts, and polar singularity thresholds—and solves them using clean, performance-optimized vector calculus. 

---

## Technical Architecture & Core Formulas

The framework separates physics execution, world data collection, and camera tracking into fully decoupled single-responsibility modules:

### 1. Dynamic Geometric Gravity (`PlanetGravity.cs`)
Instead of defaulting to a single global downward pull vector (`-9.81`), the player’s active downward force vector is evaluated dynamically inside the `GravityManager` scene lookup table. Gravity direction is calculated relative to the closest point on the active shape's mathematical surface boundaries:
- **Spherical Worlds:** Calculated via standard **Vector Normalization** straight out from the planetoid center core.
- **Cubic Worlds (Boxes):** Utilizes **Axis-Aligned Face Projection**. The script samples the player's local coordinate matrix via `InverseTransformPoint`, applies **Dominant Component Extraction** (`Mathf.Abs` comparisons), and pulls Mario flatly and perpendicularly toward the closest local polygon face. This prevents diagonal vector slippage when running past sharp 90-degree edges.
- **Cylindrical Worlds (Capsules):** Resolves orientation by executing a **1D Line-Segment Clamp** (`Mathf.Clamp`) along a single local axis, smoothly blending a cylinder profile into a sphere at the rounded end caps.

### 2. Geometric Surface Evaluation (`SurfaceNormalResolver.cs`)
Bypasses the input-twisting limitations of local perimeter multi-sampling arrays on smooth sphere primitives. It utilizes **Vector Inversion** (`-gravityDir`) to establish a rock-solid baseline core normal, combined with a single **Central Linear Raycast** passing directly down through the player's capsule midpoint to dynamically detect mesh slope variations, steps, and polygon faces.

### 3. Absolute Viewport Control Grid (`PlayerController.cs`)
To resolve the notorious **South Pole Singularity Loop**—where the tracking camera and the player controller fight to rotate each other's yaw transform properties until the input grid spins out of control—the navigation matrix is strictly decoupled:
- **Vector Projection on Plane:** Projects the screen's absolute horizontal sideways axis (`cameraTransform.right`) flat onto the standing planet tangent using `Vector3.ProjectOnPlane`. Because your computer screen always has a left and right border, this vector can never collapse or experience gimbal lock.
- **Quaternion Angle-Axis Offset:** Derives the forward travel line by rotating the clean screen-space right vector exactly 90 degrees counter-clockwise along the `playerUp` axis using `Quaternion.AngleAxis`. 
- **Absolute Screen-Space Turning Engine:** Rather than passing raw 3D vectors into a standard look-rotation matrix (which mirrors backwards when upside down), the visual mesh turning loops read the analog stick as a raw 2D screen angle using a **Bivariate Screen-Angle Conversion** (`Mathf.Atan2`). Pushing Left always guides Mario to the monitor screen's left, even when standing completely upside down on the under-belly of the globe.
- **Jump Lockout Protection:** Implements a localized temporal lockout clock (`JumpLockoutDuration`) that forces the grounding raycasts to sleep for exactly `0.12s` following takeoff, allowing the Rigidbody's upward linear velocity vectors to clear the ground snap envelope reliably across any gravity scale configurations.

### 4. Parametric Flight Transit (`LaunchStar.cs`)
Propels the player between separate planetary gravity fields. It temporarily freezes the Rigidbody's **Dynamic Forces Integration** and programmatically glides the player's coordinate space along a **Bezier Curve Matrix** using the native Unity Splines API. By updating positions via `WaitForFixedUpdate()` and scaling metrics along `Time.fixedDeltaTime`, the flight engine completely eliminates **Variable Frame Rate Jitter** and ghost blur artifacts.

### 5. Camera Tracking Horizon Buffer (`CameraUpStabilizer.cs`)
Locks Cinemachine 3.x's *Orbital Follow* component to a smooth planetary frame. It queries the active planet field and uses **Spherical Linear Interpolation** (`Vector3.Slerp`) to gently blend its local upward rotation axis toward the new planet's normal over a fraction of a second. This absorbs all chaotic visual hard snaps, converting inter-planetary transitions into fluid, sweeping cinematic curves.

---

## Controls

| Action | Input Device Map | Architecture Binding |
| :--- | :--- | :--- |
| **Move / Orbit** | `WASD` / Left Analog Stick | Screen-Space Projected Grid Matrix |
| **Jump / Launch** | `Space` / South Button | Ground-Lockout Physics Launch |
| **Camera Adjust** | `Mouse` / Right Analog Stick | Cinemachine Orbital Cam View |
| **Sprint Toggle** | `Left Shift` / Left Stick Click | Horizontal Velocity Multiplier |

## Future Improvements

- **Gravity Zones:** Custom spline-shaped bounds for directional box gravity streams.
- **Moving Planets:** Matrix parent-velocity additions to support running on rotating or translating celestial bodies.
- **Custom Editor Tooling:** Specialized inspector GUI windows to dynamically hide unneeded primitive fields based on active shape enum toggles.
- **Additional Traversal Mechanics:** Implementing custom velocity multipliers for a Wii-style shaking Spin Attack flail and ground-pound crash mechanics.

## Installation

1. Clone the repository to your local directory.
2. Open the project folder using **Unity Hub** via **Unity 6 (6000.0 LTS)** or newer.
3. Ensure the **Splines** package and **Cinemachine 3.x** are active inside the *Package Manager*.
4. Open `Assets/Scenes/GameplayScene.unity`.
5. Press **Play** in the editor.

## License

MIT License - Feel free to use this architecture framework for your own gravity-defying platformers, bootleg sandboxes, or *Super Not Luigi Universe* spin-offs!
