using Unity.Entities;
using UnityEngine;
using UnityEngine.PlayerLoop;

[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial class PrePresentationSystemGroup : ComponentSystemGroup {}

[UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
public partial class ResetAfterPresentationSystemGroup : ComponentSystemGroup {}