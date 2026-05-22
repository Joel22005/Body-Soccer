# Body Soccer — Mixed Reality Prototype

## Project Overview

Competitive **1 vs 1 mixed reality game** developed in Unity, where two players use physical motion sensors (controllers) tracked in real space to control and kick a series of pucks on a virtual field, with the objective of hitting a central ball and scoring a goal in the opponent's goal.

---

## Functional Features

### Spin Wheel — Match Start
At the beginning of each match, a visual spinning wheel determines which team starts.
- Ease-in-out animation for a smooth and dramatic spin
- Always stops at a fixed angle corresponding to the winning color
- Visual result always matches the logical result communicated to the game system
- Game starts automatically once the result is displayed

### Turn System
The game operates on a **strict turn-based system**, equivalent to chess or billiards.
- When the active player kicks, the turn automatically passes to the opponent
- Only the active team can select and kick pucks
- Visual indicators update instantly on each turn change

### Goal & Reset System
- Score is updated on the HUD when a goal is scored
- A goal animation panel is displayed with the scoring team's color
- A sound effect plays on goal
- After a **2.5 second pause**, all pucks and the ball reset to their initial positions
- The team that conceded receives the turn to kick off

### Puck Selection
- Players select the **closest puck** to their physical position
- Selection is triggered by a **crouching gesture** detected by the tracking system
- A **Line Renderer** visually highlights the selected puck
- The indicator is only visible for the active team's turn

### Kick Mechanic
- Kick **force and direction** are calculated from the player's distance and position relative to the selected puck
- The further the player stands from the puck, the greater the force applied
- Force is clamped to a configurable maximum

### Player Visuals
- Each player is represented by a **football boot sprite**
- Colored **red or blue** according to their team
- Provides clear visual identification of each player's position on the field
