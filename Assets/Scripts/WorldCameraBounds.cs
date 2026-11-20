using UnityEngine;
using Unity.Cinemachine;

// Clamp the complete viewport, not just its centre. Regions follow active interiors/layouts.
[DisallowMultipleComponent]
public sealed class WorldCameraBounds : CinemachineExtension
{
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !state.Lens.Orthographic) return;
        var target = camera.Follow;
        var region = CameraBoundaryRegion.Find(target ? target.position : state.RawPosition);
        if (region == null) return;
        var position = state.RawPosition + state.PositionCorrection;
        state.PositionCorrection += region.Confine(position, state.Lens.OrthographicSize, state.Lens.Aspect) - position;
    }
}
