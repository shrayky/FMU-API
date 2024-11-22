// Типы валидации
const ValidationType = {
    WINDOWS_PATH: 'windowsPath',
    HTTP_ADDRESS: 'httpAddress',
    HOSTNAME: 'hostname',
    COUCH_DB: 'couchDb',
    FRONTOL_DB: 'frontolDb'
};

// Базовые регулярные выражения
const BasePatterns = {
    WINDOWS_CHARS: /[\\/:*?"<>|]/,
    DIGITS: /^\d+$/,
    DOMAIN: /^[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$/
};

// Класс для создания валидаторов
class Validator {
    constructor(type, validateFn, message) {
        this.type = type;
        this.validate = (value) => {
            if (value == null) return false;
            return validateFn(String(value));
        };
        this.message = message;
    }

    // Создает конфигурацию для Webix
    toWebix() {
        return {
            validate: this.validate,
            invalidMessage: this.message,
            on: {
                onBlur: function() {
                    this.validate();
                }
            }
        };
    }
}

// Фабрика валидаторов
const createValidator = (type, validateFn, message) => {
    return new Validator(type, validateFn, message);
};

// Валидаторы путей
export const windowsPathValidator = createValidator(
    ValidationType.WINDOWS_PATH,
    (value) => {
        if (!value || value.length > 260) return false; // MAX_PATH в Windows
        return /^([a-zA-Z]:\\[^<>:"/\\|?*]+\\?)$|^(\\\\[^<>:"/\\|?*]+\\[^<>:"/\\|?*]+\\?)$/.test(value);
    },
    "Введите корректный путь к каталогу Windows"
);

// Сетевые валидаторы
export const httpAddressValidator = createValidator(
    ValidationType.HTTP_ADDRESS,
    (value) => {
        if (value == "")
            return true;

        if (!value || value.length > 2083) return false; // MAX_URL_LENGTH
        return /^https?:\/\/[a-zA-Z0-9.-]+(?::\d+)?(?:\/[^?#]*)?(?:\?[^#]*)?(?:#.*)?$/.test(value);
    },
    "Адрес должен быть в формате http(s)://hostname[:port]"
);

// Валидатор пути к базе данных фронтола
export const frontolDbPathValidator = createValidator(
    ValidationType.FRONTOL_DB,  
    (value) => {
        if (value == "")
            return true;

        let colonIndex = value.indexOf(':');

        if (colonIndex === -1)
            return false;
    
        let server = value.substring(0, colonIndex);
        let dbFilePath = value.substring(colonIndex + 1);
    
        // Проверяем наличие порта
        let path;
    
        if (/^\d+:/.test(dbFilePath)) {
            // Если есть порт, разделяем оставшуюся часть по первому двоеточию
            let portColonIndex = dbFilePath.indexOf(':');
            let port = dbFilePath.substring(0, portColonIndex);
            dbFilePath = dbFilePath.substring(portColonIndex + 1);
            
            // Проверяем, что порт является числом
            if (!/^\d+$/.test(port)) 
                return false;
        }
    
        if (!server || /[\\/:*?"<>|]/.test(server)) return false;
    
        if (dbFilePath.startsWith('/')) {
            return dbFilePath.length > 1;
        }
        
        // Если путь начинается с буквы диска, проверяем Windows-формат
        if (/^[A-Za-z]:\\/.test(dbFilePath)) {
            return true;
        }
    
        // Проверяем alias (не должен содержать слеши и двоеточия)
        return /^[^\\/:*?"<>|]+$/.test(dbFilePath);
    },
    "Путь должен быть указан как 'ИМЯ_СЕРВЕРА:ПУТЬ_К_БАЗЕ_НА_СЕРВЕРЕ'"
);

// Валидатор адреса базы данных couchDB
export const couchDbServerValidator = createValidator(
    ValidationType.COUCH_DB,  
    (value) => {
        // CouchDB требования к именам баз:
        // - только строчные буквы (a-z)
        // - цифры (0-9)
        // - специальные символы (_$()+-/)
        // - не может начинаться с подчеркивания

        if (value == "")
            return true;

        return /^[a-z][a-z0-9_$()+-/]*$/.test(value);
    },
    "Допустимы только строчные латинские буквы, цифры и символы _$()+-/"
);

// Валидатор адреса сайта
export const hostNameValidator = createValidator(
    ValidationType.HOSTNAME,  
    (value) => {
        // Проверяем формат domain.tld
        // - Может содержать буквы, цифры, дефисы
        // - Части разделены точками
        // - Минимум одна точка
        // - Не может начинаться или заканчиваться дефисом
        // - Не может содержать последовательные точки

        if (value == "")
            return true;

        return /^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$/.test(value);
    },
    "Адрес должен быть в формате domain.tld (например: au124.ru)"
);

export const windowsPathValidation = windowsPathValidator.toWebix();
export const httpAddressValidation = httpAddressValidator.toWebix();
export const frontolDbValidation = frontolDbPathValidator.toWebix();
export const couchDbNameValidation = couchDbServerValidator.toWebix();
export const hostnameValidation = hostNameValidator.toWebix();