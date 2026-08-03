// One labelled input with its inline error message, used by every validated
// form. Passing `onToggleReveal` turns it into a password field with a
// show/hide button; `hint` shows a neutral note (e.g. "Checking availability…")
// when the field has no error.
export default function Field({
  label, type = "text", error, hint, reveal, onToggleReveal, ...props
}) {
  const input = (
    <input
      className={"input" + (error ? " is-invalid" : "")}
      type={type}
      aria-invalid={Boolean(error)}
      {...props}
    />
  );

  return (
    <label className="field">
      <span>{label}</span>
      {onToggleReveal ? (
        <div className="input-wrap">
          {input}
          <button
            type="button"
            className="input-toggle"
            onClick={onToggleReveal}
            aria-label={reveal ? "Hide password" : "Show password"}
            title={reveal ? "Hide password" : "Show password"}
          >
            {reveal ? "🙈" : "👁"}
          </button>
        </div>
      ) : input}

      {error
        ? <div className="field-error"><span aria-hidden="true">⚠</span><span>{error}</span></div>
        : hint ? <div className="field-hint">{hint}</div> : null}
    </label>
  );
}
