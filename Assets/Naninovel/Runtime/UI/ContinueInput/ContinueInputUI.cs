// Copyright 2022 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class ContinueInputUI : CustomUI, IContinueInputUI
    {
        protected GameObject Trigger => trigger;

        [SerializeField] private GameObject trigger;

        private IInputManager inputManager;

        public override UniTask InitializeAsync ()
        {
            inputManager?.GetContinue()?.AddObjectTrigger(trigger);
            return UniTask.CompletedTask;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(trigger);

            inputManager = Engine.GetService<IInputManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            inputManager?.GetContinue()?.RemoveObjectTrigger(trigger);
        }
    }
}
