using NUnit.Framework;
using Core.Tests;

namespace Tests.Editor
{
    public class CoreSimulationEditModeTests
    {
        [Test]
        public void GameBoard_AllPhase1Tests_Pass()
        {
            GameBoardTests.RunAllTests();
        }

        [Test]
        public void ActionsAndEffects_AllPhase2Tests_Pass()
        {
            ActionAndEffectTests.RunAllTests();
        }

        [Test]
        public void EnemyAI_AllPhase3Tests_Pass()
        {
            EnemyAITests.RunAllTests();
        }

        [Test]
        public void Scheduling_AllPhase4Tests_Pass()
        {
            SchedulingTests.RunAllTests();
        }

        [Test]
        public void Presentation_AllPhase5Tests_Pass()
        {
            PresentationTests.RunAllTests();
        }

        [Test]
        public void Integration_AllPhase6Tests_Pass()
        {
            IntegrationTurnLoopTests.RunAllTests();
        }

        [Test]
        public void PlanC_AI_BossRoom_Keys_Pass()
        {
            PlanCTests.RunAllTests();
        }

        [Test]
        public void PlanD_MultiTilePillarsAndBarrels_Pass()
        {
            PlanDTests.RunAllTests();
        }

        [Test]
        public void PlanE_UI_HUD_Minimap_AndAudio_Pass()
        {
            PlanETests.RunAllTests();
        }
    }
}
// Trigger editor assembly recompile

