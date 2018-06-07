using System;
using System.Text;
using DeployLX.Licensing.v5;

// You can delete this section after reading
//#error Add a call to License.Required() to protect your project.
// Example:
//
// var license = new LicenseHelper();
// license.Required();
//
// Visit http://xheo.com/get-help for additional samples and guidance.


/// <summary>
///     Helper class to quickly add licensing to your project.
/// </summary>
internal class LicenseHelper : IDisposable
{
    private SecureLicense _license;

    /// <summary>
    ///     Gets a value that indicates if the current license is for a trial edition.
    /// </summary>
    public bool IsTrial
    {
        get
        {
            if (!Check(false))
                return false;
            return _license.IsTrial;
        }
    }

    /// <summary>
    ///     Gets a value that indicates if the current license has an Activation limit and it has
    ///     been activated.
    /// </summary>
    public bool IsActivated
    {
        get
        {
            if (!Check(false))
                return false;
            return _license.IsActivated;
        }
    }

    /// <summary>
    ///     Gets a value that indicates if the current license has an Activation limit.
    /// </summary>
    public bool HasActivation
    {
        get
        {
            if (!Check(false))
                return false;
            return _license.IsActivation;
        }
    }

    /// <summary>
    ///     Gets a reference to the last license validation error.
    /// </summary>
    public NoLicenseException LastError { get; private set; }

    /// <summary>
    ///     Make sure all license resources are disposed and any pending data is persisted.
    /// </summary>
    void IDisposable.Dispose()
    {
        if (_license != null)
            _license.Dispose();
        _license = null;
    }

    /// <summary>
    ///     Check for a valid license but don't throw any exceptions if a license cannot be found.
    /// </summary>
    /// <param name="silent">Indicates if DeployLX should validate without showing any forms to the user.</param>
    /// <returns>Returns true if a valid license was found, otherwise false.</returns>
    public bool Check(bool silent)
    {
        try
        {
            Required(silent);
            return true;
        }
        catch (NoLicenseException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Asks DeployLX to assert that the application has a valid license.
    /// </summary>
    /// <exception cref="DeployLX.Licensing.v5.NoLicenseException">
    ///     Thrown when a valid license could not be obtained. See
    ///     exception ValidationRecords for details.
    /// </exception>
    public void Required()
    {
        Required(false);
    }

    /// <summary>
    ///     Asks DeployLX to assert that the application has a valid license.
    /// </summary>
    /// <exception cref="DeployLX.Licensing.v5.NoLicenseException">
    ///     Thrown when a valid license could not be obtained. See
    ///     exception ValidationRecords for details.
    /// </exception>
    /// <param name="silent">Indicates if DeployLX should validate without showing any forms to the user.</param>
    public void Required(bool silent)
    {
        try
        {
            var info = new LicenseValidationRequestInfo();
            info.DontShowForms = silent;

#if DEBUG
            // See http://xheo.com/knowledge-base/deploylx/licensing/enabling-developer-mode
            info.DeveloperMode = true;

            // See http://xheo.com/knowledge-base/deploylx/licensing/how-to-testing-license-time-or-subscription-limits
            // info.TestDate = DateTime.UtcNow.AddDays( 30 );

            // See http://xheo.com/knowledge-base/deploylx/licensing/logging-license-validations
            info.Logger = DefaultLicenseLogger.Instance.Log;
#endif

            var license = SecureLicenseManager.Validate(this, null, info);

            if (_license != null && license != _license)
                _license.Dispose();

            _license = license;

            LastError = null;
        }
        catch (NoLicenseException ex)
        {
            LastError = ex;
            RecordLicenseError(ex);
            throw;
        }
    }

    /// <summary>
    ///     Called when a valid license could not be found when calling <see cref="Required" /> or <see cref="Check" />.
    /// </summary>
    /// <param name="ex">Exception reported by DeployLX.</param>
    public void RecordLicenseError(NoLicenseException ex)
    {
#warning Log license validation errors. 
    }

    /// <summary>
    ///     Gets a string representation of the license ownership data.
    /// </summary>
    public string GetLicenseInfo()
    {
        var info = new StringBuilder();

        // Check for a license but don't prompt the user if the license hasn't been registered yet.
        if (!Check(true))
            return "Not licensed.";

        info.AppendLine(_license.Type);

        if (_license.Username != null)
            info.AppendLine(_license.Username);

        if (_license.Organization != null)
            info.AppendLine(_license.Organization);

        if (_license.IsUnlocked)
            info.AppendLine(_license.SerialNumber);

        if (_license.IsActivation && !_license.IsActivated)
            info.AppendLine("Not Activated");

        return info.ToString();
    }

    /// <summary>
    ///     Activates the license on the current machine.
    /// </summary>
    /// <returns>
    ///     Returns true if the license was activated, otherwise false.
    /// </returns>
    public bool Activate()
    {
        // Hardware Locking and the Activation Process
        // http://xheo.com/docs/DLXl/5/html/developers%20guide/licensing%20basics/hardware%20locking%20and%20the%20activation%20process.html


        if (!Check(false))
            return false;

        var activation = _license.Limits[typeof(ActivationLimit)] as ActivationLimit;
        if (activation == null)
            return false;

        if (activation.HasActivated)
            return true;

        try
        {
            var info = new LicenseValidationRequestInfo();
#if DEBUG
            // See http://xheo.com/knowledge-base/deploylx/licensing/enabling-developer-mode
            info.DeveloperMode = true;
#endif

            var newLicense = SecureLicenseManager.ShowForm(activation, null, this, null, info, null);
            if (newLicense != null && newLicense != _license)
            {
                if (_license != null)
                    _license.Dispose();
                _license = newLicense;
            }

            return true;
        }
        catch (SecureLicenseException)
        {
            // TODO: Decide how to handle failures here. Might parse exception or just
            // ignore and assume DeployLX error reporting to user was enough.
        }

        return false;
    }

    /// <summary>
    ///     Deactivates the license from the current machine.
    /// </summary>
    /// <returns>
    ///     Returns true if the license was deacivated, otherwise false.
    /// </returns>
    public bool Deactivate()
    {
        if (_license == null)
            return false;

        var info = new LicenseValidationRequestInfo();
#if DEBUG
        // See http://xheo.com/knowledge-base/deploylx/licensing/enabling-developer-mode
        info.DeveloperMode = true;
#endif

        return SecureLicenseManager.Deactivate(_license, this, null, info);
    }

    /// <summary>
    ///     Shows the registration form to the user so they can enter a new serial number. Might be used
    ///     to upgrade to a better edition or fix a wrongly entered serial number.
    /// </summary>
    /// <returns>
    ///     Returns true if the registration unlocked a new license, otherwise false.
    /// </returns>
    public bool ReShowRegistrationForm()
    {
        // Need to validate the license first. 
        if (_license == null)
            return false;

        var info = new LicenseValidationRequestInfo();

#if DEBUG
        // See http://xheo.com/knowledge-base/deploylx/licensing/enabling-developer-mode
        info.DeveloperMode = true;
#endif

        var newLicense = _license.ShowRegistrationForm(this, null, info);
        if (newLicense != null && newLicense != _license)
        {
            _license.Dispose();
            _license = newLicense;
            return true;
        }


        return false;
    }
}