namespace FSO.SimAntics.JIT.Translation.Model
{
    public enum PrimitiveReturnType
    {
        SimanticsTrue, //VMPrimitiveExitCode, interpreter fallback
        SimanticsTrueFalse, //VMPrimitiveExitCode, interpreter fallback 
        SimanticsSubroutine, //VMPrimitiveExitCode
        SimanticsStatement, //result in _sResult.

        NativeStatementTrue, //eg. c#: "test = 1;"
        NativeExpressionTrueFalse, //eg. c#: "(test == 1)."
        NativeStatementTrueFalse //result in _bResult after one or more statements. eg. c#: "{ code; code; _bResult = true; }" 
    }
}
