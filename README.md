# Survival-Ai
Unity ML-Agents survival simulation where an AI-controlled player learns to collect wood, survive enemies, manage health, and complete objectives through reinforcement learning, rewards, curriculum difficulty, and real-time environment resets.


# Survival-AI

Survival-AI is a Unity-based reinforcement learning project built with Unity ML-Agents. The project simulates a survival environment where an AI-controlled player learns how to move, collect wood, avoid danger, fight enemies, and complete an objective through trial and error.

The main agent is controlled by `PlayerAgent`, which acts as the bridge between the Unity game environment and the ML-Agents training system. The agent receives observations from the environment, such as health, collected wood, nearby trees, nearby enemies, water detection, and raycast information. Based on these observations, the model outputs continuous actions for movement and attack decisions.

The training process is guided by a reward system that encourages useful behavior, such as collecting wood, hitting enemies, killing enemies, and reaching the target objective. Negative rewards discourage dying, wasting time, walking on water, and attacking without success.

The project also includes a curriculum difficulty system through `DifficultyCurriculumMapper`, which adjusts runtime gameplay parameters according to the training difficulty. This allows the environment to become progressively harder as the agent improves.

## Key Features

- Unity ML-Agents integration
- AI-controlled player using reinforcement learning
- Observation system based on health, wood, nearby objects, enemies, water detection, and raycasts
- Continuous action space for movement and attack intent
- Reward-based learning system
- Dynamic episode reset system
- Runtime difficulty curriculum
- Enemy AI with perception, movement, attack states, health, and damage system
- Harvesting system with collectible wood drops
- Modular player, enemy, harvesting, and reset architecture
- TensorBoard-compatible training workflow through ML-Agents

## Main Systems

### Player Agent System

The `PlayerAgent` script is the core of the machine learning logic. It collects observations, receives actions from the trained model, applies movement and attacks, gives rewards, and ends episodes when the player wins or dies.

### Environment Reset System

`EpisodeResetManager` resets the environment at the beginning of each episode. It clears old enemies and drops, resets trees, restores the player state, and spawns enemies according to the current difficulty.

### Curriculum System

`DifficultyCurriculumMapper` creates a runtime copy of the base configuration and modifies gameplay values based on the training difficulty. This allows the environment to scale progressively during training.

### Harvesting System

The harvesting system allows the agent to attack trees, generate wood drops, and collect wood through `WoodTracker`. Collecting enough wood can complete the episode objective.

### Enemy System

Enemies use perception, movement, attack logic, hitboxes, health, and animations to create dynamic threats for the player. The agent receives rewards for successfully hitting or killing enemies.

## Machine Learning Overview

The project can be described as a reinforcement learning problem where:

- **State**: the agent’s observations from the environment
- **Actions**: movement on the X/Z axes and attack intent
- **Rewards**: positive or negative feedback based on the agent’s behavior
- **Episode**: one survival attempt ending in success or failure

The agent learns by maximizing its total expected reward over many episodes.

## Technologies Used

- Unity
- C#
- Unity ML-Agents
- PyTorch backend through ML-Agents
- TensorBoard for training visualization

## Project Goal

The goal of this project is to demonstrate how reinforcement learning can be applied to a survival game environment. The agent must learn to balance exploration, resource collection, combat, and survival in a dynamic world.
