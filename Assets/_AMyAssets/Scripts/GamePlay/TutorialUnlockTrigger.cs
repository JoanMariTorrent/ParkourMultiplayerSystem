using UnityEngine;

public class TutorialUnlockTrigger : MonoBehaviour
{
    public enum AbilityToUnlock {Jump, Sprint, Slide, WallRun, WallClimb}
    public AbilityToUnlock abilityToUnlock;
    public int index;
    public bool giveAbility;


    void OnTriggerEnter(Collider other)
    {
        other.TryGetComponent(out PlayerCharacter pc);
        if(pc == null) return;

        if(giveAbility)
        {
            switch (abilityToUnlock)
            {
                case AbilityToUnlock.Jump:
                pc.canJumpTutorial = true;
                break;

                case AbilityToUnlock.Sprint:
                pc.canSprintTutorial = true;
                break;

                case AbilityToUnlock.Slide:
                pc.canSlideTutorial = true;
                break;

                case AbilityToUnlock.WallRun:
                pc.canWallRunTutorial = true;
                break;

                case AbilityToUnlock.WallClimb:
                pc.canClimbTutorial = true;
                break;
            }
        }
        

        var tutorialView = pc.GetComponentInChildren<TutorialView>();
        if(tutorialView != null)
        {
            tutorialView.ShowView(index);
            Debug.Log("ASODJNASOJNDJINAOSDNJIOASDJOINASDJNIO");
        }

        gameObject.SetActive(false);
    }
}
