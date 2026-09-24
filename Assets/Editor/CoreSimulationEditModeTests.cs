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
    }
}
