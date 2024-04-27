using System.Collections.Generic;
using Studio;
using KKAPI.Utilities;
using KKAPI.Studio.SaveLoad;

using RSkoi_ComponentUtil.UI;

namespace RSkoi_ComponentUtil.Scene
{
    internal class ComponentUtilSceneBehaviour : SceneCustomFunctionController
    {
        protected override void OnSceneLoad(SceneOperationKind operation, ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            // TODO:
        }

        protected override void OnSceneSave()
        {
            // TODO:
        }

        protected override void OnObjectsSelected(List<ObjectCtrlInfo> objectCtrlInfo)
        {
            // force singular selection
            if (objectCtrlInfo.Count > 1 || objectCtrlInfo.Count == 0)
                return;

            if (ComponentUtilUI._canvasContainer.activeSelf)
                ComponentUtil._instance.Entry(objectCtrlInfo[0].guideObject.transformTarget.gameObject);

            base.OnObjectsSelected(objectCtrlInfo);
        }
    }
}
