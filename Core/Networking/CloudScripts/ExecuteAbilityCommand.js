handlers.ExecuteAbilityCommand = async function (args, context) {
    const { matchId, commandData } = args;
    
    // Validate input
    if (!matchId || !commandData) {
        return {
            success: false,
            errorMessage: "Invalid command data: missing matchId or commandData"
        };
    }

    // Extract command data
    const { unitId, abilityIndex, targetX, targetY, playerId } = commandData;
    
    try {
        // Get current game state
        const gameState = await server.GetGameState(matchId);
        if (!gameState) {
            return {
                success: false,
                errorMessage: "Game state not found"
            };
        }

        // Find the unit
        const unit = gameState.Units.find(u => u.UnitId === unitId.toString());
        if (!unit) {
            return {
                success: false,
                errorMessage: `Unit ${unitId} not found`
            };
        }

        // Verify ownership/turn
        const isPlayer1 = playerId === gameState.Player1.PlayerId;
        if ((isPlayer1 && !unit.IsPlayer1Unit) || (!isPlayer1 && unit.IsPlayer1Unit)) {
            return {
                success: false,
                errorMessage: "Cannot use ability: Unit does not belong to the player"
            };
        }
        if (isPlayer1 !== gameState.IsPlayer1Turn) {
            return {
                success: false,
                errorMessage: "Cannot use ability: Not player's turn"
            };
        }

        // Verify phase
        if (gameState.CurrentPhase !== TurnPhase.Aura) {
            return {
                success: false,
                errorMessage: "Cannot use ability: Not in aura phase"
            };
        }

        // Verify unit can act
        if (unit.HasUsedAbility) {
            return {
                success: false,
                errorMessage: "Unit has already used an ability this turn"
            };
        }
        if (unit.HasPlannedAbility) {
            return {
                success: false,
                errorMessage: "Unit already has a planned ability"
            };
        }

        // Get ability data
        if (abilityIndex < 0 || abilityIndex >= unit.Abilities.length) {
            return {
                success: false,
                errorMessage: "Invalid ability index"
            };
        }
        const ability = unit.Abilities[abilityIndex];

        // Check cooldown
        if (ability.IsOnCooldown) {
            return {
                success: false,
                errorMessage: "Ability is on cooldown"
            };
        }

        // Check aura cost
        const player = isPlayer1 ? gameState.Player1 : gameState.Player2;
        if (player.CurrentAura < ability.AuraCost) {
            return {
                success: false,
                errorMessage: "Insufficient aura"
            };
        }

        // Check range
        const distance = Math.abs(targetX - unit.Position.x) + Math.abs(targetY - unit.Position.y); // Manhattan distance
        if (distance > ability.Range) {
            return {
                success: false,
                errorMessage: "Target is out of range"
            };
        }

        // Check target validity
        const targetUnit = gameState.Units.find(u => 
            u.Position.x === targetX && 
            u.Position.y === targetY
        );

        // Targeting validation
        if (targetUnit) {
            // Self-targeting check
            if (targetUnit.UnitId === unit.UnitId && !ability.TargetsSelf) {
                return {
                    success: false,
                    errorMessage: "Cannot target self with this ability"
                };
            }

            // Ally/Enemy targeting check
            const isTargetAlly = targetUnit.IsPlayer1Unit === unit.IsPlayer1Unit;
            if (isTargetAlly && !ability.TargetsAlly) {
                return {
                    success: false,
                    errorMessage: "Cannot target allies with this ability"
                };
            }
            if (!isTargetAlly && !ability.TargetsEnemy) {
                return {
                    success: false,
                    errorMessage: "Cannot target enemies with this ability"
                };
            }
        } else if (!ability.TargetsGround) {
            return {
                success: false,
                errorMessage: "Cannot target empty space with this ability"
            };
        }

        // All validations passed - update unit state with planned ability
        unit.HasPlannedAbility = true;
        unit.PlannedAbilityIndex = abilityIndex;
        unit.PlannedAbilityTarget = { x: targetX, y: targetY };

        // Save updated game state
        await server.UpdateGameState(matchId, gameState);

        // Return success with updated state
        return {
            success: true,
            gameState: gameState
        };

    } catch (error) {
        server.LogError("ExecuteAbilityCommand error: " + error.message);
        return {
            success: false,
            errorMessage: "Internal server error"
        };
    }
}; 