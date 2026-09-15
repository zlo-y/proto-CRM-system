namespace Manager.BusinessLogic.Exceptions;

// 
//  Ошибки приложения, включая NotFoundException, ConflictException, ValidationAppException, AuthenticationFailedException и ForbiddenException.
// 
public class NotFoundException: Exception
{
    public NotFoundException(string message) : base(message){}
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message){}
}

public class ValidationAppException:  Exception
{
    public ValidationAppException(string message) : base(message){}
}

public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message = "Неверный email или пароль"): base(message){}
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "У вас нет доступа к этому ресурсу.") : base(message){}
}