namespace FSO.SimAntics.Model.Platform
{
    public abstract class VMAbstractLotState : VMPlatformState
    {
        public VMAbstractValidator Validator;
        public virtual bool LimitExceeded { get { return false; } set { } }

        public virtual bool CanPlaceNewUserObject(VM vm)
        {
            return true;
        }

        public virtual bool CanPlaceNewDonatedObject(VM vm)
        {
            return false;
        }

        public abstract void ActivateValidator(VM vm);
        public VMAbstractLotState() { }
        public VMAbstractLotState(int version) : base(version) { }
    }
}
