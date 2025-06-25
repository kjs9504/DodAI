using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR.Hands;

public class HandAnchorUpdater : MonoBehaviour
{
    public Handedness handedness = Handedness.Left;
    public XRHandJointID jointID = XRHandJointID.Palm;

    XRHandSubsystem GetSubsystem()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        // ���� ���� �� ������ �װ�, ������ ù ��°
        return subsystems.FirstOrDefault(s => s.running) ?? subsystems.FirstOrDefault();
    }

    void Update()
    {
        var subsystem = GetSubsystem();
        if (subsystem == null || !subsystem.running)
            return;

        var hand = (handedness == Handedness.Left)
            ? subsystem.leftHand
            : subsystem.rightHand;

        if (hand.GetJoint(jointID).TryGetPose(out Pose p))
            transform.SetPositionAndRotation(p.position, p.rotation);
    }
}

