/// <summary>Settings 參數對照表單列（四欄）。</summary>
public struct ParameterComparisonRow
{
    public string category;
    public string parameter;
    public string preset1;
    public string control;
    public bool isHeader;

    public ParameterComparisonRow(string category, string parameter, string preset1, string control, bool isHeader = false)
    {
        this.category = category ?? "";
        this.parameter = parameter ?? "";
        this.preset1 = preset1 ?? "";
        this.control = control ?? "";
        this.isHeader = isHeader;
    }
}
