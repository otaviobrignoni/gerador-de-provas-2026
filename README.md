# Gerador de Provas

Desenvolvido durante o curso Fullstack da [Academia do Programador 2026](https://www.academiadoprogramador.net)

## Especificação funcional

Cada módulo apresenta primeiro as entidades e suas propriedades. Em seguida, são descritas as regras de negócio e os comportamentos esperados.

### 1. Módulo de disciplinas

#### Entidade `Disciplina`

| Propriedade | Descrição                           |
| ----------- | ----------------------------------- |
| `ID`        | Identificador da disciplina.        |
| `Nome`      | Nome da disciplina.                 |
| `Matérias`  | Matérias relacionadas à disciplina. |
| `Testes`    | Testes relacionados à disciplina.   |

#### Regras de negócio

##### Cadastro

- O campo `Nome` é obrigatório.
- Não é permitido cadastrar duas disciplinas com o mesmo nome.

##### Edição

- O campo `Nome` é obrigatório.
- Não é permitido editar uma disciplina para utilizar o mesmo nome de outra disciplina.

##### Exclusão

- Não deve ser possível excluir disciplinas que possuam matérias ou testes relacionados.

##### Listagem

A listagem de disciplinas deve exibir:

- `ID`;
- `Nome`.

### 2. Módulo de matérias

#### Entidade `Matéria`

| Propriedade  | Descrição                             |
| ------------ | ------------------------------------- |
| `ID`         | Identificador da matéria.             |
| `Nome`       | Nome da matéria.                      |
| `Disciplina` | Disciplina à qual a matéria pertence. |
| `Série`      | Série escolar relacionada à matéria.  |
| `Questões`   | Questões relacionadas à matéria.      |

#### Regras de negócio

##### Cadastro

- Os campos `Nome`, `Disciplina` e `Série` são obrigatórios.
- Não é permitido cadastrar duas matérias com o mesmo nome.

##### Edição

- Os campos `Nome`, `Disciplina` e `Série` são obrigatórios.
- Não é permitido editar uma matéria para utilizar o mesmo nome de outra matéria.

##### Exclusão

- Não deve ser possível excluir matérias utilizadas em questões.

##### Listagem

A listagem de matérias deve exibir:

- `ID`;
- `Nome`;
- `Disciplina`;
- `Série`.

### 3. Módulo de questões

#### Entidade `Questão`

| Propriedade        | Descrição                                            |
| ------------------ | ---------------------------------------------------- |
| `ID`               | Identificador da questão.                            |
| `Matéria`          | Matéria à qual a questão pertence.                   |
| `Enunciado`        | Texto apresentado no início da questão.              |
| `Alternativas`     | Conjunto de alternativas disponíveis para a questão. |
| `Resposta correta` | Alternativa correta da questão.                      |
| `Testes`           | Testes que utilizam a questão.                       |

#### Entidade `Alternativa`

| Propriedade | Descrição                                     |
| ----------- | --------------------------------------------- |
| `Texto`     | Texto da alternativa.                         |
| `Correta`   | Indica se a alternativa é a resposta correta. |

#### Regras de negócio

##### Cadastro

- Os campos `Matéria`, `Enunciado` e `Alternativas` são obrigatórios.
- Cada questão deve ter uma quantidade mínima e máxima de alternativas. O máximo sugerido é de quatro alternativas.
- Devem ser configuradas, no mínimo, duas alternativas.
- Não deve ser possível cadastrar questões sem uma alternativa correta.
- Não deve ser possível cadastrar mais de uma alternativa correta.

##### Edição

- Os campos `Matéria`, `Enunciado` e `Alternativas` são obrigatórios.
- Cada questão deve ter uma quantidade mínima e máxima de alternativas. O máximo sugerido é de quatro alternativas.
- Devem ser configuradas, no mínimo, duas alternativas.
- Não deve ser possível manter uma questão sem uma alternativa correta.
- Não deve ser possível manter mais de uma alternativa correta.

##### Configuração de alternativas

- Deve ser possível adicionar alternativas à questão.
- Deve ser possível remover alternativas da questão.
- As regras de quantidade mínima e de alternativa correta devem ser respeitadas após cada alteração.

##### Exclusão

- Não deve ser possível excluir questões relacionadas a um teste.

##### Listagem

A listagem de questões deve exibir:

- `ID`;
- `Enunciado`;
- `Matéria`;
- `Resposta correta`.

### 4. Módulo de testes

#### Entidade `Teste`

| Propriedade              | Descrição                                                    |
| ------------------------ | ------------------------------------------------------------ |
| `ID`                     | Identificador do teste.                                      |
| `Título`                 | Título do teste.                                             |
| `Disciplina`             | Disciplina selecionada para o teste.                         |
| `Matéria`                | Matéria selecionada para o teste, quando aplicável.          |
| `Série`                  | Série escolar do teste.                                      |
| `Quantidade de questões` | Número de questões que devem ser geradas.                    |
| `Prova de recuperação`   | Indica se o teste considera todas as matérias da disciplina. |
| `Questões`               | Questões selecionadas para compor o teste.                   |

#### Regras de negócio

##### Geração

- Os campos `Título`, `Disciplina`, `Matéria`, `Série` e `Quantidade de questões` são obrigatórios.
- Deve ser informada a quantidade de questões que será gerada.
- Não é permitido cadastrar dois testes com o mesmo título.
- A quantidade informada deve ser menor ou igual à quantidade de questões cadastradas.
- As matérias devem ser carregadas a partir da disciplina selecionada.
- Não deve ser possível selecionar uma matéria que não pertença à disciplina selecionada.
- Caso a disciplina seja alterada, o campo `Matéria` deve ser limpo.
- Em uma **Prova de Recuperação**, devem ser consideradas as questões de todas as matérias da disciplina selecionada.
- As questões devem ser selecionadas aleatoriamente.

##### Duplicação

- Deve ser possível duplicar testes.
- Na duplicação, `Disciplina`, `Quantidade de questões`, `Série`, `Prova de recuperação` e `Matéria` devem vir preenchidos.
- Não é permitido duplicar um teste com o mesmo título.
- Na duplicação, as questões devem vir em branco.

##### Exclusão

- Deve ser possível excluir testes existentes.

##### Listagem

A listagem de testes deve exibir:

- `ID`;
- `Título`;
- `Disciplina`;
- `Matéria` ou indicação de que é uma prova de recuperação;
- `Quantidade de questões`.

##### Detalhes

- Deve ser possível visualizar cada teste individualmente.
- A visualização deve apresentar informações detalhadas, incluindo as questões.

##### PDF do teste

O arquivo PDF do teste deve apresentar:

- `Título`;
- `Disciplina`;
- `Matéria`;
- Questões;
- Alternativas.

##### PDF do gabarito

O arquivo PDF do gabarito deve apresentar:

- `Título`;
- `Disciplina`;
- `Matéria`;
- Questões;
- Alternativas, com a alternativa correta assinalada.

## Como utilizar

1. Clone o repositório ou baixe o código-fonte.
2. Abra o terminal ou o prompt de comando e navegue até a pasta raiz da solução.
3. Restaure as dependências:

   ```bash
   dotnet restore
   ```

4. Execute o projeto com compilação em tempo real:

   ```bash
   dotnet run --project src/GeradorDeProvas.WebApp
   ```

## Requisitos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Licenças de fontes

Este projeto distribui a fonte JetBrains Mono para geração de PDFs.

- Fonte: [JetBrains Mono](https://github.com/JetBrains/JetBrainsMono)
- Licença: SIL Open Font License 1.1 (OFL-1.1)
- Texto da licença no repositório: [LICENSES/JetBrainsMono-OFL.txt](LICENSES/JetBrainsMono-OFL.txt)
