using System;
using System.Collections.Generic;
using System.Linq;
using CabinetBilder.Core.Machining;
using CabinetBilder.Core.Skeletons;

namespace CabinetBilder.Core.Skeletons
{
    /// <summary>
    /// Service to calculate and apply drilling operations to Skeleton components.
    /// </summary>
    public static class DrillingService
    {
        private const double System32_FrontOffset = 37.0;
        private const double System32_RearOffset = 37.0;

        public static void ApplyDrilling(Skeleton skeleton)
        {
            var components = skeleton.Components.ToList();
            
            // For now, let's focus on Side and Bottom/Top connections
            var sides = components.Where(c => c.Name.Contains("Side")).ToList();
            var topsBottoms = components.Where(c => c.Name == "Bottom" || c.Name == "Top").ToList();

            foreach (var side in sides)
            {
                foreach (var horiz in topsBottoms)
                {
                    GenerateJunctionDrilling(side, horiz);
                }
            }
        }

        private static void GenerateJunctionDrilling(SkeletonComponent side, SkeletonComponent horiz)
        {
            // Simplified detection: 
            // Side is vertical (X is thickness), Horiz is horizontal (Z is thickness).
            // They meet if Horiz.PosX == Side.PosX + Side.Thickness (or Side.PosX)
            
            bool isLeftConnection = Math.Abs(horiz.PosX - (side.PosX + side.Thickness)) < 0.1;
            bool isRightConnection = Math.Abs(side.PosX - (horiz.PosX + horiz.Width)) < 0.1;

            if (isLeftConnection || isRightConnection)
            {
                // We have a junction. Let's add drillings.
                // 1. Face drilling on the side
                // 2. Edge drilling on the horizontal panel
                
                double zPos = horiz.PosZ + (horiz.Thickness / 2);
                
                // Front hole
                AddDrillingPair(side, horiz, System32_FrontOffset, zPos, isLeftConnection);
                
                // Rear hole
                AddDrillingPair(side, horiz, horiz.Height - System32_RearOffset, zPos, isLeftConnection);
                
                // If deep enough, add middle hole
                if (horiz.Height > 400)
                {
                    AddDrillingPair(side, horiz, horiz.Height / 2, zPos, isLeftConnection);
                }
            }
        }

        private static void AddDrillingPair(SkeletonComponent side, SkeletonComponent horiz, double yOnPanel, double zOnSkeleton, bool isSideAtStart)
        {
            // Drilling on Side (Face)
            // Local coords for side: 
            // - X = thickness? No, local X depends on Normal.
            // For sides, Normal=(1,0,0), Dir=(0,1,0). Local X=Width (Depth), Local Y=Height.
            // Wait, let's check Skeleton.cs update.
            // Side: Width = d (Y), Height = h (Z). Normal = (1,0,0), Dir = (0,1,0).
            // Local X is along Dir (Y), Local Y is along Up (Z).
            
            side.Operations.Add(new DrillOperation
            {
                Name = "Dowel Face",
                X = yOnPanel,
                Y = zOnSkeleton,
                Z = isSideAtStart ? side.Thickness : 0, // Depth from face
                Diameter = 8.0,
                Depth = 12.0,
                NormalX = 0, NormalY = 0, NormalZ = 1 // Face normal in local space
            });

            // Drilling on Horizontal (Edge)
            // Horiz: Width = w-2t (X), Height = d (Y). Normal = (0,0,1), Dir = (1,0,0).
            // Local X is along Dir (X), Local Y is along Up (Y).
            // Edge drilling is on the side edge (X=0 or X=Width).
            
            horiz.Operations.Add(new DrillOperation
            {
                Name = "Dowel Edge",
                X = isSideAtStart ? 0 : horiz.Width,
                Y = yOnPanel,
                Z = horiz.Thickness / 2,
                Diameter = 8.0,
                Depth = 30.0,
                NormalX = isSideAtStart ? -1 : 1, // Edge normal in local space
                NormalY = 0, NormalZ = 0
            });
        }
    }
}
