using System.Globalization;

namespace FastVitae;

// One language choice
public record LanguageOption(string Code, string Name, string FlagIso)
{
    public string Flag => $"pack://application:,,,/FamFamFam.Flags.Wpf;component/images/{FlagIso}.png";
}

// App and CV words, translated
public static class Text
{
    public static readonly List<LanguageOption> Languages = new()
    {
        new("es", "Español", "es"),
        new("pt", "Português", "br"),
        new("en", "English", "us")
    };

    public static string Language { get; private set; } = "en";

    public static event Action? LanguageChanged;

    public static void SetLanguage(string code)
    {
        Language = Languages.Any(l => l.Code == code) ? code : "en";
        LanguageChanged?.Invoke();
    }

    // Windows language, else English
    public static string SystemLanguage()
    {
        string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return code is "es" or "pt" ? code : "en";
    }

    // English in, current language out
    public static string Get(string english)
    {
        if (Language == "en" || !Table.TryGetValue(english, out var words))
            return english;
        return Language == "es" ? words.Es : words.Pt;
    }

    // Translated text with values
    public static string Format(string english, params object[] values) => string.Format(Get(english), values);

    private static readonly Dictionary<string, (string Es, string Pt)> Table = new()
    {
        // ---- CV words ----
        ["January"] = ("Enero", "Janeiro"),
        ["February"] = ("Febrero", "Fevereiro"),
        ["March"] = ("Marzo", "Março"),
        ["April"] = ("Abril", "Abril"),
        ["May"] = ("Mayo", "Maio"),
        ["June"] = ("Junio", "Junho"),
        ["July"] = ("Julio", "Julho"),
        ["August"] = ("Agosto", "Agosto"),
        ["September"] = ("Septiembre", "Setembro"),
        ["October"] = ("Octubre", "Outubro"),
        ["November"] = ("Noviembre", "Novembro"),
        ["December"] = ("Diciembre", "Dezembro"),
        ["Present"] = ("Actualidad", "Atual"),
        ["AREAS OF EXPERTISE"] = ("ÁREAS DE ESPECIALIZACIÓN", "ÁREAS DE ESPECIALIZAÇÃO"),
        ["PROFESSIONAL EXPERIENCE"] = ("EXPERIENCIA PROFESIONAL", "EXPERIÊNCIA PROFISSIONAL"),
        ["EDUCATION"] = ("EDUCACIÓN", "FORMAÇÃO ACADÊMICA"),
        ["ADDITIONAL SKILLS"] = ("HABILIDADES ADICIONALES", "HABILIDADES ADICIONAIS"),

        // ---- Statuses ----
        ["Applied"] = ("Postulado", "Candidatado"),
        ["In process"] = ("En proceso", "Em andamento"),
        ["Approved"] = ("Aprobado", "Aprovado"),
        ["Rejected"] = ("Rechazado", "Recusado"),

        // ---- Buttons and labels ----
        ["+ Add Section"] = ("+ Agregar sección", "+ Adicionar seção"),
        ["+ Column"] = ("+ Columna", "+ Coluna"),
        ["+ Entry"] = ("+ Entrada", "+ Entrada"),
        ["+ Role"] = ("+ Cargo", "+ Cargo"),
        ["A Specialty"] = ("Una Especialidad", "Uma Especialidade"),
        ["Add"] = ("Agregar", "Adicionar"),
        ["Add Section"] = ("Agregar sección", "Adicionar seção"),
        ["Add a bullet to this column"] = ("Agregar un punto a esta columna", "Adicionar um item a esta coluna"),
        ["Add a column, up to four"] = ("Agregar una columna, hasta cuatro", "Adicionar uma coluna, até quatro"),
        ["Add a company, school or project"] = ("Agregar una empresa, institución o proyecto", "Adicionar uma empresa, instituição ou projeto"),
        ["Add a kind of job, for example: Programming"] = ("Agregar un tipo de trabajo, por ejemplo: Programación", "Adicionar um tipo de trabalho, por exemplo: Programação"),
        ["Add a specialty inside the selected Field, for example: C#"] = ("Agregar una especialidad dentro del Área elegida, por ejemplo: C#", "Adicionar uma especialidade na Área escolhida, por exemplo: C#"),
        ["Add another role in the same place"] = ("Agregar otro cargo en el mismo lugar", "Adicionar outro cargo no mesmo lugar"),
        ["An already existing CV"] = ("Un CV que ya existe", "Um CV que já existe"),
        ["Applications"] = ("Postulaciones", "Candidaturas"),
        ["Basic info"] = ("Datos básicos", "Dados básicos"),
        ["Browse..."] = ("Examinar...", "Procurar..."),
        ["Bullet"] = ("Punto", "Item"),
        ["Bullet Columns"] = ("Columnas de puntos", "Colunas de itens"),
        ["Bullets, one per line"] = ("Puntos, uno por línea", "Itens, um por linha"),
        ["Cancel"] = ("Cancelar", "Cancelar"),
        ["Change the status"] = ("Cambiar el estado", "Mudar o status"),
        ["Choose..."] = ("Elegir...", "Escolher..."),
        ["City, country"] = ("Ciudad, país", "Cidade, país"),
        ["Company"] = ("Empresa", "Empresa"),
        ["Company, school or project"] = ("Empresa, institución o proyecto", "Empresa, instituição ou projeto"),
        ["Continue"] = ("Continuar", "Continuar"),
        ["Country code"] = ("Código de país", "Código do país"),
        ["Create"] = ("Crear", "Criar"),
        ["Data folder"] = ("Carpeta de datos", "Pasta de dados"),
        ["Date applied"] = ("Fecha de postulación", "Data da candidatura"),
        ["Delete"] = ("Eliminar", "Excluir"),
        ["Edit"] = ("Editar", "Editar"),
        ["Edit the content saved in this Specialty"] = ("Editar el contenido guardado en esta Especialidad", "Editar o conteúdo salvo nesta Especialidade"),
        ["Edit your basic info"] = ("Editar tus datos básicos", "Editar seus dados básicos"),
        ["Email address"] = ("Correo electrónico", "E-mail"),
        ["End month"] = ("Mes de fin", "Mês de término"),
        ["End year, or Present"] = ("Año de fin, o Actualidad", "Ano de término, ou Atual"),
        ["Fast Vitae - Your details"] = ("Fast Vitae - Tus datos", "Fast Vitae - Seus dados"),
        ["Field"] = ("Área", "Área"),
        ["Fields"] = ("Áreas", "Áreas"),
        ["First name"] = ("Nombre", "Nome"),
        ["For example: PROJECTS"] = ("Por ejemplo: PROYECTOS", "Por exemplo: PROJETOS"),
        ["For example: Software Developer"] = ("Por ejemplo: Desarrollador de Software", "Por exemplo: Desenvolvedor de Software"),
        ["Format"] = ("Formato", "Formato"),
        ["Height"] = ("Alto", "Altura"),
        ["Job title or degree"] = ("Cargo o título", "Cargo ou diploma"),
        ["Keep this content so new CVs can start from it"] = ("Guardar este contenido para empezar nuevos CV desde él", "Guardar este conteúdo para começar novos CVs a partir dele"),
        ["Language"] = ("Idioma", "Idioma"),
        ["Last edit"] = ("Última edición", "Última edição"),
        ["Last name"] = ("Apellido", "Sobrenome"),
        ["Like AREAS OF EXPERTISE: short bullets in three columns"] = ("Como ÁREAS DE ESPECIALIZACIÓN: puntos cortos en columnas", "Como ÁREAS DE ESPECIALIZAÇÃO: itens curtos em colunas"),
        ["Like PROFESSIONAL EXPERIENCE: a name, a place, dates and bullets"] = ("Como EXPERIENCIA PROFESIONAL: nombre, lugar, fechas y puntos", "Como EXPERIÊNCIA PROFISSIONAL: nome, local, datas e itens"),
        ["LinkedIn link"] = ("Enlace de LinkedIn", "Link do LinkedIn"),
        ["Location"] = ("Ubicación", "Localização"),
        ["Month"] = ("Mes", "Mês"),
        ["Move down"] = ("Bajar", "Descer"),
        ["Move up"] = ("Subir", "Subir"),
        ["Name and Bulletpoints"] = ("Nombre y puntos", "Nome e itens"),
        ["Name and title on the left, your picture on the right"] = ("Nombre y título a la izquierda, tu foto a la derecha", "Nome e título à esquerda, sua foto à direita"),
        ["New CV"] = ("Nuevo CV", "Novo CV"),
        ["No CVs yet"] = ("Todavía no hay CV", "Ainda não há CVs"),
        ["No applications yet"] = ("Todavía no hay postulaciones", "Ainda não há candidaturas"),
        ["Open"] = ("Abrir", "Abrir"),
        ["Open folder"] = ("Abrir carpeta", "Abrir pasta"),
        ["Open in folder"] = ("Abrir en la carpeta", "Abrir na pasta"),
        ["Open the CVs folder"] = ("Abrir la carpeta de CV", "Abrir a pasta de CVs"),
        ["Open the CV you applied with"] = ("Abrir el CV con el que se postuló", "Abrir o CV usado na candidatura"),
        ["Optional. Shown on CVs that have Picture turned on."] = ("Opcional. Se muestra en los CV con Foto activada.", "Opcional. Aparece nos CVs com Foto ativada."),
        ["Phone number"] = ("Teléfono", "Telefone"),
        ["Pick a Field or type a new one, for example: Programming"] = ("Elige un Área o escribe una nueva, por ejemplo: Programación", "Escolha uma Área ou digite uma nova, por exemplo: Programação"),
        ["Pick a Specialty or type a new one, for example: C#"] = ("Elige una Especialidad o escribe una nueva, por ejemplo: C#", "Escolha uma Especialidade ou digite uma nova, por exemplo: C#"),
        ["Picture"] = ("Foto", "Foto"),
        ["Profile"] = ("Perfil", "Perfil"),
        ["Profiles ▾"] = ("Perfiles ▾", "Perfis ▾"),
        ["Remove"] = ("Quitar", "Remover"),
        ["Remove this bullet"] = ("Quitar este punto", "Remover este item"),
        ["Remove this column"] = ("Quitar esta columna", "Remover esta coluna"),
        ["Remove this entry"] = ("Quitar esta entrada", "Remover esta entrada"),
        ["Remove this role"] = ("Quitar este cargo", "Remover este cargo"),
        ["Remove this section"] = ("Quitar esta sección", "Remover esta seção"),
        ["Rename"] = ("Renombrar", "Renomear"),
        ["Role"] = ("Cargo", "Cargo"),
        ["Save"] = ("Guardar", "Salvar"),
        ["Save Folder"] = ("Carpeta de guardado", "Pasta de salvamento"),
        ["Save to Specialty"] = ("Guardar en Especialidad", "Salvar na Especialidade"),
        ["Saved content per kind of job, to start new CVs from"] = ("Contenido guardado por tipo de trabajo, para empezar nuevos CV", "Conteúdo salvo por tipo de trabalho, para começar novos CVs"),
        ["Scratch"] = ("Cero", "Zero"),
        ["Section title"] = ("Título de la sección", "Título da seção"),
        ["Show one Field"] = ("Mostrar un Área", "Mostrar uma Área"),
        ["Show one status"] = ("Mostrar un estado", "Mostrar um status"),
        ["Specialties"] = ("Especialidades", "Especialidades"),
        ["Specialty"] = ("Especialidad", "Especialidade"),
        ["Start"] = ("Empezar", "Começar"),
        ["Start from"] = ("Empezar desde", "Começar a partir de"),
        ["Start month"] = ("Mes de inicio", "Mês de início"),
        ["Start year"] = ("Año de inicio", "Ano de início"),
        ["Status"] = ("Estado", "Status"),
        ["Summary"] = ("Resumen", "Resumo"),
        ["Switch, add or delete profiles"] = ("Cambiar, agregar o eliminar perfiles", "Trocar, adicionar ou excluir perfis"),
        ["The company you are applying to"] = ("La empresa a la que te postulas", "A empresa para a qual você se candidata"),
        ["The position you are applying for in that company"] = ("El puesto al que te postulas en esa empresa", "A vaga para a qual você se candidata nessa empresa"),
        ["This file could not be read"] = ("No se pudo leer este archivo", "Não foi possível ler este arquivo"),
        ["Title"] = ("Título", "Título"),
        ["Two to four lines about who you are. Leave empty to skip it."] = ("De dos a cuatro líneas sobre quién eres. Déjalo vacío para omitirlo.", "De duas a quatro linhas sobre quem você é. Deixe vazio para omitir."),
        ["Where Fast Vitae keeps your profiles and CVs"] = ("Donde Fast Vitae guarda tus perfiles y CV", "Onde o Fast Vitae guarda seus perfis e CVs"),
        ["Where you live, or where you are looking to relocate"] = ("Donde vives, o adonde quieres mudarte", "Onde você mora, ou para onde quer se mudar"),
        ["Write the CV as PDF and Word into the company folder"] = ("Crear el CV en PDF y Word en la carpeta de la empresa", "Criar o CV em PDF e Word na pasta da empresa"),
        ["Year"] = ("Año", "Ano"),
        ["Your basic info"] = ("Tus datos básicos", "Seus dados básicos"),
        ["Change where CVs are saved"] = ("Cambiar dónde se guardan los CV", "Mudar onde os CVs são salvos"),
        ["Folder"] = ("Carpeta", "Pasta"),
        ["Only CVs created from now on"] = ("Solo los CV creados de ahora en adelante", "Só os CVs criados a partir de agora"),
        ["The Fast Vitae CV folders will be moved to the new folder."] =
            ("Las carpetas de CV de Fast Vitae se moverán a la carpeta nueva.", "As pastas de CV do Fast Vitae serão movidas para a pasta nova."),
        ["The existing Fast Vitae CV folders will stay in the old folder."] =
            ("Las carpetas de CV que ya existen se quedan en la carpeta anterior.", "As pastas de CV que já existem ficam na pasta anterior."),
        ["Your profile has no picture, so you will need to provide one."] =
            ("No tienes una foto en tu perfil, por lo que deberás proporcionar una.", "Seu perfil não tem foto, então você precisará fornecer uma."),
        ["Apply to all dates"] = ("Aplicar a todas las fechas", "Aplicar a todas as datas"),
        ["Apply to all titles"] = ("Aplicar a todos los cargos", "Aplicar a todos os cargos"),
        ["Apply to all companies"] = ("Aplicar a todas las empresas", "Aplicar a todas as empresas"),
        ["Apply to all locations"] = ("Aplicar a todas las ubicaciones", "Aplicar a todas as localizações"),
        ["Text format"] = ("Formato de texto", "Formato do texto"),
        ["Font"] = ("Fuente", "Fonte"),
        ["Size"] = ("Tamaño", "Tamanho"),
        ["Bold"] = ("Negrita", "Negrito"),
        ["Italic"] = ("Cursiva", "Itálico"),
        ["Strikethrough"] = ("Tachado", "Tachado"),
        ["Reset"] = ("Restablecer", "Redefinir"),
        ["Change picture..."] = ("Cambiar foto...", "Trocar foto..."),

        // ---- Messages ----
        ["Please enter a title."] = ("Escribe un título.", "Digite um título."),
        ["Save this content"] = ("Guardar este contenido", "Salvar este conteúdo"),
        ["Saved"] = ("Guardado", "Salvo"),
        ["Created in"] = ("Creado en", "Criado em"),
        ["The files could not be written. If the PDF or Word file is open in another program, close it and try again."] =
            ("No se pudieron escribir los archivos. Si el PDF o el Word están abiertos en otro programa, ciérralos y vuelve a intentarlo.",
             "Não foi possível gravar os arquivos. Se o PDF ou o Word estiver aberto em outro programa, feche e tente de novo."),
        ["The CV was created, but the applications list could not be updated. If its Excel file is open, close it and click Create again."] =
            ("El CV se creó, pero no se pudo actualizar la lista de postulaciones. Si su Excel está abierto, ciérralo y haz clic en Crear otra vez.",
             "O CV foi criado, mas a lista de candidaturas não foi atualizada. Se o Excel estiver aberto, feche e clique em Criar de novo."),
        ["The PDF was not created because the preview is not available. The Word file was created."] =
            ("El PDF no se creó porque la vista previa no está disponible. El Word sí se creó.",
             "O PDF não foi criado porque a pré-visualização não está disponível. O Word foi criado."),
        ["The PDF could not be written:"] = ("No se pudo escribir el PDF:", "Não foi possível gravar o PDF:"),
        ["Saved to {0} - {1}"] = ("Guardado en {0} - {1}", "Salvo em {0} - {1}"),
        ["The Specialty could not be saved:"] = ("No se pudo guardar la Especialidad:", "Não foi possível salvar a Especialidade:"),
        ["Your changes were not saved. Close anyway?"] = ("Tus cambios no se guardaron. ¿Cerrar de todos modos?", "Suas alterações não foram salvas. Fechar mesmo assim?"),
        ["This CV has changes that were not created. Close anyway?"] = ("Este CV tiene cambios que no se crearon. ¿Cerrar de todos modos?", "Este CV tem alterações que não foram criadas. Fechar mesmo assim?"),
        ["Add Field"] = ("Agregar Área", "Adicionar Área"),
        ["Field name"] = ("Nombre del Área", "Nome da Área"),
        ["Rename Field"] = ("Renombrar Área", "Renomear Área"),
        ["Delete the Field {0} and all its Specialties?"] = ("¿Eliminar el Área {0} y todas sus Especialidades?", "Excluir a Área {0} e todas as suas Especialidades?"),
        ["Add a Field first."] = ("Primero agrega un Área.", "Adicione uma Área primeiro."),
        ["Add Specialty"] = ("Agregar Especialidad", "Adicionar Especialidade"),
        ["Specialty name"] = ("Nombre de la Especialidad", "Nome da Especialidade"),
        ["That Specialty already exists."] = ("Esa Especialidad ya existe.", "Essa Especialidade já existe."),
        ["This Specialty could not be read, so it was left untouched:"] = ("No se pudo leer esta Especialidad, así que no se tocó:", "Não foi possível ler esta Especialidade, então ela não foi alterada:"),
        ["Rename Specialty"] = ("Renombrar Especialidad", "Renomear Especialidade"),
        ["Delete the Specialty {0}?"] = ("¿Eliminar la Especialidad {0}?", "Excluir a Especialidade {0}?"),
        ["That could not be done:"] = ("No se pudo hacer:", "Não foi possível fazer isso:"),
        ["All fields"] = ("Todas las Áreas", "Todas as Áreas"),
        ["All statuses"] = ("Todos los estados", "Todos os status"),
        ["The applications could not be read:"] = ("No se pudieron leer las postulaciones:", "Não foi possível ler as candidaturas:"),
        ["The status could not be saved. If the Excel file is open, close it and try again."] =
            ("No se pudo guardar el estado. Si el Excel está abierto, ciérralo y vuelve a intentarlo.",
             "Não foi possível salvar o status. Se o Excel estiver aberto, feche e tente de novo."),
        ["The CV file was not found:"] = ("No se encontró el archivo del CV:", "O arquivo do CV não foi encontrado:"),
        ["This file could not be read, so it was left untouched:"] = ("No se pudo leer este archivo, así que no se tocó:", "Não foi possível ler este arquivo, então ele não foi alterado:"),
        ["Fast Vitae - Profile"] = ("Fast Vitae - Perfil", "Fast Vitae - Perfil"),
        ["There is already a profile called {0}."] = ("Ya existe un perfil llamado {0}.", "Já existe um perfil chamado {0}."),
        ["The profile could not be saved:"] = ("No se pudo guardar el perfil:", "Não foi possível salvar o perfil:"),
        ["Add profile..."] = ("Agregar perfil...", "Adicionar perfil..."),
        ["Delete {0}..."] = ("Eliminar {0}...", "Excluir {0}..."),
        ["Fast Vitae - New profile"] = ("Fast Vitae - Nuevo perfil", "Fast Vitae - Novo perfil"),
        ["This is the only profile. Add another one before deleting it."] = ("Este es el único perfil. Agrega otro antes de eliminarlo.", "Este é o único perfil. Adicione outro antes de excluí-lo."),
        ["Delete the profile {0} with all its CVs and Fields? This cannot be undone."] =
            ("¿Eliminar el perfil {0} con todos sus CV y Áreas? No se puede deshacer.",
             "Excluir o perfil {0} com todos os seus CVs e Áreas? Isso não pode ser desfeito."),
        ["This Field has no Specialties yet"] = ("Esta Área todavía no tiene Especialidades", "Esta Área ainda não tem Especialidades"),
        ["Please enter the company, the role and the Field."] = ("Escribe la empresa, el cargo y el Área.", "Digite a empresa, o cargo e a Área."),
        ["You already have a CV for this company and role. It will be opened."] = ("Ya tienes un CV para esta empresa y cargo. Se abrirá.", "Você já tem um CV para esta empresa e cargo. Ele será aberto."),
        ["Please enter a Field and a Specialty."] = ("Escribe un Área y una Especialidad.", "Digite uma Área e uma Especialidade."),
        ["{0} - {1} already has saved content. Replace it?"] = ("{0} - {1} ya tiene contenido guardado. ¿Reemplazarlo?", "{0} - {1} já tem conteúdo salvo. Substituir?"),
        ["Choose your picture"] = ("Elige tu foto", "Escolha sua foto"),
        ["That file could not be opened as a picture."] = ("Ese archivo no se pudo abrir como foto.", "Esse arquivo não pôde ser aberto como foto."),
        ["Folder where Fast Vitae keeps your data"] = ("Carpeta donde Fast Vitae guarda tus datos", "Pasta onde o Fast Vitae guarda seus dados"),
        ["Please enter your first and last name."] = ("Escribe tu nombre y apellido.", "Digite seu nome e sobrenome."),
        ["Please choose a folder for your data."] = ("Elige una carpeta para tus datos.", "Escolha uma pasta para seus dados."),
        ["The data folder was not found: {0}"] = ("No se encontró la carpeta de datos: {0}", "A pasta de dados não foi encontrada: {0}"),
        ["That folder could not be used:"] = ("No se pudo usar esa carpeta:", "Não foi possível usar essa pasta:"),
        ["The settings could not be saved:"] = ("No se pudo guardar la configuración:", "Não foi possível salvar as configurações:"),
        ["The folder was not found:"] = ("No se encontró la carpeta:", "A pasta não foi encontrada:"),
        ["Please choose a folder."] = ("Elige una carpeta.", "Escolha uma pasta."),
        ["The data could not be moved:"] = ("No se pudieron mover los datos:", "Não foi possível mover os dados:"),
        ["Fast Vitae - Save Folder"] = ("Fast Vitae - Carpeta de guardado", "Fast Vitae - Pasta de salvamento"),
        ["The folder is not empty:"] = ("La carpeta no está vacía:", "A pasta não está vazia:"),
        ["The new folder cannot be inside the current one."] = ("La carpeta nueva no puede estar dentro de la actual.", "A nova pasta não pode ficar dentro da atual."),
        ["Pictures"] = ("Imágenes", "Imagens"),
        ["All files"] = ("Todos los archivos", "Todos os arquivos"),
        ["The preview and the PDF need the Microsoft Edge WebView2 Runtime, which was not found on this PC."] =
            ("La vista previa y el PDF necesitan Microsoft Edge WebView2 Runtime, que no se encontró en esta PC.",
             "A pré-visualização e o PDF precisam do Microsoft Edge WebView2 Runtime, que não foi encontrado neste PC."),
        ["The picture could not be saved:"] = ("No se pudo guardar la foto:", "Não foi possível salvar a foto:"),
    };
}
