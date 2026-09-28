using System;
using System.Collections.Generic;
using Core.Board;
using Core.Effects;
using Core.Occupants;
using UnityEngine;

namespace Core.Actions
{
    public class JumpAction : IBoardAction
    {
        public TileOccupant Occupant { get; }
        public Vector2Int Destination { get; }
        public float ArcHeight { get; }

        public JumpAction(TileOccupant occupant, Vector2Int destination, float arcHeight = 1.0f)
        {
            Occupant = occupant ?? throw new ArgumentNullException(nameof(occupant));
            Destination = destination;
            ArcHeight = arcHeight;
        }

        public List<BoardEffect> Execute(GameBoard board)
        {
            var effects = new List<BoardEffect>();
            if (board == null || Occupant == null) return effects;

            Vector2Int startPos = Occupant.GridPosition;
            if (startPos == Destination) return effects;
            
            if (!board.CanEnter(Destination))
            {
                return effects;
            }

            bool moved = board.Move(Occupant, Destination);
            if (moved)
            {
                effects.Add(new JumpEffect(Occupant.Id, startPos, Destination, ArcHeight));
            }

            return effects;
        }

        public override string ToString()
        {
            return $"JumpAction: Occupant {Occupant?.Id} from {Occupant?.GridPosition} -> {Destination} (Arc: {ArcHeight:F1})";
        }
    }
}
