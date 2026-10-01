# InclusiRH — Especificação de telas para UI/UX

Guia de orientação para o desenho do **App Mobile (Flutter)**, usado por colaboradores, e do **Painel Web (ASP.NET MVC)**, usado por RH, comitê de DEI e administração.

Este documento descreve jornadas, conteúdo de cada tela, estados vazios e de erro, e o contrato da API que a interface deve consumir. Não substitui o wireframe: define o que cada tela precisa comunicar e o que não pode expor.

---

## 1. Visão geral do projeto

O InclusiRH é uma plataforma corporativa de Diversidade, Equidade e Inclusão. Ela reúne três compromissos da empresa:

- **Escuta ativa:** canal de denúncias com anonimato opcional e validação bilateral da tratativa.
- **Autodeclaração protegida:** dados demográficos sensíveis, com consentimento e cifra em repouso, usados só de forma agregada pelo RH.
- **Letramento:** trilhas de aprendizagem, com destaque para as Leis nº 10.639/2003 e nº 11.645/2008, e taxa de conclusão dos cursos obrigatórios no pilar Social do ESG.

A API já existe em ASP.NET Core. Os dois front-ends consomem os mesmos endpoints. O que muda é **quem entra** e **o que a tela tem permissão de mostrar**.

| Frente | Quem usa | Objetivo da interface |
|---|---|---|
| App Mobile (Flutter) | Colaborador e Gestor | Declarar, denunciar, acompanhar o próprio caso e concluir trilhas |
| Painel Web (ASP.NET MVC) | RH, Comitê DEI e Admin | Tratar denúncias, ver indicadores agregados e auditar treinamentos obrigatórios |

### 1.1 Papéis e para onde a pessoa vai depois do login

O login devolve `papel`. A interface deve encaminhar a sessão conforme esse valor. Os apelidos `RH` e `Admin` existem na API; nas telas, prefira os rótulos abaixo.

| Valor da API | Rótulo na interface | Destino |
|---|---|---|
| `Colaborador` | Colaborador | App Mobile |
| `Gestor` | Gestor | App Mobile (mesma jornada do colaborador) |
| `RecursosHumanos` | RH | Painel Web |
| `ComiteDei` | Comitê DEI | Painel Web, com foco em denúncias contestadas e escalonadas |
| `Administrador` | Administração | Painel Web, com as mesmas visões de RH e o cadastro de trilhas |

A sessão usa dois tokens. O `accessToken` (JWT) vale 15 minutos e vai em toda chamada. O `refreshToken` vale 7 dias e só serve para `POST /api/auth/refresh`. Diante de HTTP 401, o app renova a sessão em silêncio e repete a chamada. A tela de login só volta se a renovação também falhar. Cada renovação devolve um par novo, e o refresh token antigo deixa de valer. Não guarde a senha no aparelho.

### 1.2 Princípios visuais que a API impõe

Estas regras não são preferência de layout. Se a tela as ignorar, a interface quebra a LGPD ou o anonimato.

1. **Dado sensível individual não aparece para o RH.** Etnia, gênero, orientação sexual, deficiência e faixa socioeconômica só são vistas pela própria pessoa. O painel mostra percentuais.
2. **Grupo com menos de 5 pessoas não vira gráfico.** A API omite o indicador e manda `motivoSupressao` ou `avisoSupressao`. A tela mostra o aviso, não um número inventado e não “0%”.
3. **Denúncia anônima não revela quem abriu**, nas listas e nos detalhes vistos por RH, comitê e administração. O app da própria pessoa continua mostrando o caso dela.
4. **Protocolo é a referência pública do caso.** Formato `INC-aaaammdd-XXXXXXXX`. Destaque-o e permita copiar.
5. **Conclusão de treinamento não volta atrás.** Depois de `Concluido`, a interface não oferece “desfazer”.
6. **Tom de voz.** Direto, respeitoso e sem alarde. Evite ícones de alerta exagerados em relatos de assédio. O canal precisa parecer seguro, não policial.

### 1.3 Estados globais

Toda tela que chama a API precisa destes estados:

| Situação | HTTP | O que a pessoa vê |
|---|---|---|
| Access token vencido | 401 | Nada. O app renova a sessão e repete a chamada |
| Sessão expirada (a renovação também falhou) | 401 | “Entre novamente para continuar.” Botão para o login |
| Papel sem permissão | 403 | “Esta área é restrita ao RH.” Sem detalhe do que existe por trás |
| Validação de formulário | 400 | Mensagem no campo (`errors` do corpo) |
| Regra de negócio | 422 | Faixa no topo com o texto de `erro` |
| Recurso inexistente ou oculto | 404 | “Não encontramos este registro.” Não diga se o protocolo existe e foi escondido |
| Falha de rede ou 500 | — | “Não foi possível concluir agora. Tente de novo.” |

---

## 2. Jornada do App Mobile (Flutter)

Público: colaborador e gestor. O app não trata denúncia de outra pessoa, não abre relatório ESG e não cadastra trilha.

```text
Login
  → Dashboard
       → Autodeclaração
       → Canal de escuta (lista e abertura)
            → Detalhe e validação bilateral
       → Trilhas de aprendizagem
            → Conteúdo do curso e progresso
```

### 2.1 Tela de autenticação

**Objetivo.** Identificar a pessoa e guardar o token.

**Endpoint.** `POST /api/auth/login`

**Campos.**

| Campo na tela | JSON | Regra |
|---|---|---|
| E-mail | `email` | Obrigatório, formato de e-mail |
| Senha | `senha` | Obrigatória, oculta, com ação de mostrar/ocultar |

**Ação principal.** “Entrar”.

**Resposta usada pela interface.** `accessToken`, `accessTokenExpiraEm`, `refreshToken`, `refreshTokenExpiraEm`, `colaboradorId`, `nome`, `papel`. Guarde os dois tokens no armazenamento seguro do aparelho. Use `nome` na saudação do dashboard. Se `papel` for RH, comitê ou administração, mostre um aviso curto: “Sua gestão fica no painel web.” O app ainda pode abrir a jornada pessoal dessa pessoa.

**Estados.**

- Credencial inválida (401): “E-mail ou senha inválidos.” Não diga qual dos dois falhou.
- Conta inativa: a API devolve a mesma frase. Não crie uma mensagem diferente.
- Sucesso: ir ao dashboard.

**Fora desta tela.** O autocadastro público (`POST /api/auth/register`) só cria o papel Colaborador e não faz parte da primeira versão do app. O primeiro acesso da empresa é feito pelo time técnico ou pelo painel, não por um botão “Criar conta” no celular.

### 2.2 Dashboard do colaborador

**Objetivo.** Atalhos para as três tarefas da pessoa, com o estado mais urgente visível sem abrir outra tela.

**Conteúdo.**

- Saudação com `nome`.
- Cartão **Minha autodeclaração.** Se `GET /api/autodeclaracao` responder 404, o texto é “Ainda não declarada” e o botão é “Declarar com privacidade”. Se existir, mostre só a data `dataAtualizacao`, nunca etnia ou deficiência neste cartão.
- Cartão **Canal de escuta.** Atalho para abrir um relato e atalho para “Meus relatos”. Se houver algum caso com status `Tratado`, um selo pede atenção: “Há uma resposta do RH para você avaliar.”
- Cartão **Trilhas.** Quantidade de cursos obrigatórios ainda não concluídos, quando o app conseguir cruzar `GET /api/treinamentos` com `GET /api/colaboradores/{colaboradorId}/treinamentos`.
- Rodapé discreto com o protocolo em destaque apenas quando a pessoa acabou de abrir um relato (ver tela de abertura).

**Endpoints de apoio.**

| Atalho | Chamada |
|---|---|
| Saber se já declarou | `GET /api/autodeclaracao` |
| Saber se há tratativa pendente de validação | `GET /api/denuncias` e filtrar `status = Tratado` |
| Ver trilhas e o próprio progresso | `GET /api/treinamentos` e `GET /api/colaboradores/{colaboradorId}/treinamentos` |

O `colaboradorId` vem do login. Não peça para a pessoa digitar o próprio identificador.

### 2.3 Tela de autodeclaração LGPD

**Objetivo.** A pessoa preenche ou atualiza os próprios dados de diversidade, com consentimento explícito antes de enviar.

**Endpoints.**

| Ação | Chamada |
|---|---|
| Abrir a tela com o que já foi salvo | `GET /api/autodeclaracao` |
| Gravar ou atualizar | `POST /api/autodeclaracao` |

**Introdução da tela, em texto fixo.** Explicar, em poucas linhas, que os dados são sensíveis, ficam cifrados, não aparecem no crachá nem na lista do RH, e só entram em percentuais de grupos com pelo menos 5 pessoas. Citar a LGPD sem transformar a tela num texto jurídico.

**Campos, nesta ordem.**

| Campo na tela | JSON | Opções que a interface deve oferecer |
|---|---|---|
| Cor ou raça | `etnia` | Branca, Preta, Parda, Amarela, Indígena, Prefiro não informar |
| Gênero | `genero` | Mulher, Homem, Mulher trans, Homem trans, Não binário, Outro, Prefiro não informar |
| Orientação sexual | `orientacaoSexual` | Heterossexual, Homossexual, Bissexual, Assexual, Pansexual, Outro, Prefiro não informar |
| Pessoa com deficiência | `temDeficiencia` | Sim (`true`), Não (`false`), Prefiro não informar (`null`) |
| Descrição da deficiência | `descricaoDeficiencia` | Texto livre, até 200 caracteres. Só aparece e só é enviado quando a resposta for Sim |
| Contexto socioeconômico | `contextoSocioeconomico` | Até 2 salários mínimos; De 2 a 5; De 5 a 10; Acima de 10; Prefiro não informar |
| Consentimento | `consentimentoLgpd` | Caixa desmarcada por padrão. Obrigatória para habilitar “Salvar” |

Os valores JSON correspondentes estão no contrato da API (`Branca`, `MulherTrans`, `AteDoisSalariosMinimos`, e assim por diante). A tela mostra a frase em português; o app envia o valor da API.

**Ação principal.** “Salvar declaração”. Permanece desabilitada enquanto `consentimentoLgpd` for falso. Se a pessoa enviar mesmo assim, a API responde 422: “O registro de dados sensíveis exige consentimento explícito, conforme a LGPD.”

**Depois de salvar.** Confirmação breve e retorno ao dashboard. Mostre `dataAtualizacao` no formato local.

**Privacidade na própria tela.** Não há foto, nome de gestor nem comparação com a média da empresa. Não pré-preencha com dados de outro sistema de RH.

### 2.4 Tela de canal de escuta e denúncias

Duas superfícies: a **lista dos meus relatos** e o **formulário de abertura**.

#### Lista

**Endpoint.** `GET /api/denuncias`

A pessoa vê apenas os próprios casos, inclusive os anônimos que ela abriu. Cada item mostra protocolo, tipo, status e data. Não mostre o nome de outras pessoas. Toque abre o detalhe.

Estados de status, com rótulo e cor sugerida (a cor é apoio, o texto é obrigatório):

| `status` | Rótulo | Significado para a pessoa |
|---|---|---|
| `Pendente` | Pendente | O RH ainda não registrou a tratativa |
| `EmAnalise` | Em análise | O caso está sendo examinado |
| `Escalonado` | Escalonado | O prazo levou o caso a uma instância superior |
| `Tratado` | Aguardando sua avaliação | Há parecer do RH. A pessoa precisa confirmar ou contestar |
| `Contestado` | Contestado | A pessoa não aceitou a tratativa. O caso foi escalonado |
| `Encerrado` | Encerrado | A pessoa confirmou a tratativa |

**Vazio.** “Você ainda não abriu um relato.” Botão “Abrir relato”.

#### Abertura

**Endpoint.** `POST /api/denuncias`, enviado como `multipart/form-data`.

| Campo na tela | Campo do formulário | Regra |
|---|---|---|
| Tipo da ocorrência | `tipoOcorrencia` | Lista: Assédio moral, Assédio sexual, Discriminação étnico-racial, Discriminação de gênero, Discriminação por orientação sexual, Capacitismo, Retaliação, Outro |
| Relato | `descricao` | Obrigatório, até 4.000 caracteres, contador visível |
| Quero que meu nome não apareça para o RH | `isAnonima` | Interruptor. Desligado por padrão, para a escolha ser consciente |
| Anexar evidências | `evidencias` (repetido) | Opcional. Até 5 arquivos de 10 MB. PDF, PNG, JPG, MP3, M4A, WAV ou OGG |

**Texto ao lado do interruptor.** “O RH não verá seu nome. Você continua podendo acompanhar este relato neste aparelho e avaliar a resposta.”

**Anexos.** Ofereça três origens: câmera, galeria e arquivo, além de gravar áudio. Mostre cada anexo como um cartão com ícone do tipo, nome, tamanho e botão de remover antes do envio. Bloqueie no aparelho o sexto arquivo e os acima de 10 MB. Se a API recusar um arquivo (422), mostre o `erro` junto do cartão daquele arquivo: a mensagem já cita o nome.

**Aviso de anonimato nos anexos.** Com o interruptor de anonimato ligado, mostre acima dos anexos: “Fotos, prints e documentos podem mostrar seu nome, rosto ou voz. Revise antes de enviar.” Em relato anônimo, o RH vê os arquivos como `evidencia-1.png`, `evidencia-2.pdf`, sem o nome original, mas a API não altera o conteúdo do arquivo.

**Ação principal.** “Enviar relato”.

**Depois do sucesso (201).** Tela de confirmação, não a lista direto.

- Protocolo em tipo grande, com botão “Copiar”.
- Frase: “Guarde este protocolo. Ele identifica o seu relato.”
- Se `isAnonima` for verdadeiro, repetir que o nome não será exibido ao RH.
- Ação secundária: “Ver relato”.

Não peça `colaboradorId` no formulário. A API usa a pessoa do token.

### 2.5 Tela de detalhes da denúncia e validação bilateral

**Objetivo.** A pessoa lê o que aconteceu com o caso e, quando o RH já tratou, confirma ou contesta.

**Endpoint de leitura.** `GET /api/denuncias/{protocolo}`

Use o protocolo guardado na lista. A resposta traz `id`, usado nas ações seguintes, `descricao`, `status`, `isAnonima`, `justificativaContestacao`, `dataValidacao`, `historicos` e `evidencias`.

**Bloco superior.** Protocolo, tipo, status e data de abertura.

**Evidências.** Lista de `evidencias` com `nomeArquivo`, `tipoArquivo`, `tamanhoBytes` e `origem` (“Enviada na abertura” ou “Enviada na contestação”). Tocar num item baixa o arquivo por `url`, com o token no cabeçalho. Imagem abre numa pré-visualização; áudio, num player simples; PDF, no visualizador do sistema. Não guarde o arquivo baixado na galeria nem em pasta pública do aparelho.

**Linha do tempo.** Um item para cada elemento de `historicos`, em ordem cronológica. Cada item mostra data, autor, texto (`parecerRH`) e, se houver, a referência textual do RH (`evidenciasAnexadas`). Quando uma evidência tiver `historicoTramitacaoId`, mostre o anexo dentro do item da contestação correspondente.

Enquanto o status for `Pendente`, `EmAnalise` ou `Escalonado`, não mostre os botões de validação. Um texto explica: “Quando o RH registrar a tratativa, você poderá confirmar ou contestar.”

#### Quando o status for `Tratado`

Dois botões no fim da tela, com o parecer mais recente visível acima deles.

| Botão | Efeito | Endpoint |
|---|---|---|
| Confirmar tratativa | Envia `statusValidacao: "Confirmado"` | `PUT /api/denuncias/{id}/validacao` |
| Contestar | Abre o campo de justificativa e só então envia | `PUT /api/denuncias/{id}/validacao` |

**Confirmar.** Diálogo curto: “Ao confirmar, o relato será encerrado.” Sucesso: status passa a `Encerrado` e os botões somem.

**Contestar.**

| Campo | JSON | Regra |
|---|---|---|
| Por que a tratativa não resolve o relato? | `justificativaContestacao` | Obrigatória neste caminho, até 4.000 caracteres |
| Novas evidências | `evidencias` (repetido) | Opcional, mesmos formatos e limites da abertura |
| — | `statusValidacao` | Valor fixo `"Contestado"` |

As duas ações enviam `multipart/form-data`. Anexos só aparecem no caminho de contestação: ao confirmar com arquivo, a API responde 422.

Sem justificativa, a API responde 422. Não deixe o botão “Enviar contestação” ativo com o campo vazio.

**Depois da contestação.** A resposta traz `alertaEscalonamentoDisparado: true` e `mensagemAlerta`. O e-mail formal à diretoria sai minutos depois, por um serviço em segundo plano; a resposta confirma que o encaminhamento está garantido, não que o e-mail já foi entregue. Mostre uma confirmação: “Sua contestação foi registrada e será encaminhada ao RH superior e à diretoria.” Não prometa prazo que a API não devolve. O status na tela passa a `Contestado`.

**Quando já estiver `Encerrado` ou `Contestado`.** Somente leitura. Se houver justificativa, mostre `justificativaContestacao` como texto fixo, não num formulário editável. O histórico só registra que houve contestação: o texto da justificativa fica cifrado no caso e não é copiado para a linha do tempo.

**O que esta tela não faz.** Não há campo de parecer do RH. Esse campo é do painel web (`PUT /api/denuncias/{id}/tratativa`).

### 2.6 Trilhas de aprendizagem

**Objetivo.** A pessoa vê os cursos disponíveis, com destaque para o letramento étnico-racial, e marca o próprio andamento.

**Lista.** `GET /api/treinamentos`

Cada cartão mostra:

- `titulo`
- `categoriaDescricao` (para letramento étnico-racial, o texto já cita as Leis 10.639/03 e 11.645/08)
- `cargaHoraria` em horas
- `modulo`
- selo “Obrigatório” quando `obrigatorio` for verdadeiro
- status da pessoa, cruzando com o progresso dela

**Progresso da pessoa.** `GET /api/colaboradores/{colaboradorId}/treinamentos`

| `status` | Rótulo no cartão | Botão |
|---|---|---|
| ausente ou `NaoIniciado` | Não iniciado | “Começar” |
| `EmAndamento` | Em andamento | “Marcar como concluído” |
| `Concluido` | Concluído em `{dataConclusao}` | Nenhum. Não oferecer desfazer |

**Detalhe do curso.** `descricao`, `modulo` e `conteudo` em leitura. O conteúdo é texto, não um player de vídeo.

**Atualizar andamento.** `POST /api/treinamentos/{id}/progresso`

| Gesto | JSON |
|---|---|
| Começar | `{ "status": "EmAndamento" }` |
| Concluir | `{ "status": "Concluido" }` |

A API grava `dataInicio` ao iniciar e `dataConclusao` ao concluir. Se a pessoa tentar voltar de concluído para em andamento, a API responde 422. A tela não deve oferecer esse caminho.

**Filtro sugerido.** “Obrigatórios” e “Letramento étnico-racial” (`categoria = LetramentoEtnicoRacial`), além de “Todos”.

**Vazio.** “Nenhuma trilha disponível no momento.”

---

## 3. Jornada do Painel Web (ASP.NET MVC)

Público: RH, comitê DEI e administração. O painel é desktop, com tabela, filtro e detalhe lado a lado ou em página própria.

```text
Login administrativo
  → Dashboard ESG e conformidade
  → Fila de denúncias
       → Detalhe protegido e parecer do RH
  → Escalonamento (contestados e escalonados)
  → Cadastro de trilhas (RH e Admin)
```

Quem tiver papel Colaborador ou Gestor e abrir o painel vê a mensagem de acesso restrito e não a fila.

### 3.1 Tela de login administrativo

**Objetivo.** Entrada no painel e bloqueio de quem não gere o canal.

**Endpoint.** O mesmo `POST /api/auth/login`.

**Campos.** E-mail e senha, iguais ao app.

**Depois do sucesso.**

| `papel` | Destino |
|---|---|
| `RecursosHumanos` ou `Administrador` | Dashboard ESG |
| `ComiteDei` | Fila filtrada em contestados e escalonados |
| `Colaborador` ou `Gestor` | Tela “Use o aplicativo InclusiRH”. Não carregue a fila |

**Sessão.** Envie o `accessToken` em `Authorization: Bearer` em todas as chamadas. Em 401, chame `POST /api/auth/refresh` e repita a chamada. Volte ao login só se a renovação falhar.

### 3.2 Dashboard ESG e conformidade

**Objetivo.** Mostrar indicadores agregados e anonimizados para a gestão, como base demográfica da Lei nº 14.611/2023 e do Estatuto da Igualdade Racial (Lei nº 12.288/2010), mais a cobertura dos treinamentos obrigatórios.

**Quem vê.** RH e Admin. O comitê não precisa desta tela na primeira versão.

**Endpoints.**

| Bloco | Chamada |
|---|---|
| Demografia | `GET /api/autodeclaracao/relatorios-esg` |
| Treinamentos | `GET /api/treinamentos/relatorios-conclusao` |

#### Bloco demográfico

Cabeçalho com `geradoEm`, `totalColaboradoresAtivos`, `totalAutodeclaracoes` e `percentualCobertura`.

Quatro grupos, nesta ordem:

1. Diversidade étnico-racial, com o número `percentualPopulacaoNegra` (preta + parda, entre quem declarou cor ou raça) quando ele vier preenchido.
2. Equidade de gênero.
3. Inclusão de PCD, com `percentualPcd` quando vier preenchido.
4. Contexto socioeconômico.

Para cada grupo, leia `liberado`.

| `liberado` | O que desenhar |
|---|---|
| `true` | Barras ou tabela com `categoria`, `quantidade` e `percentual` |
| `false` | Cartão neutro com `motivoSupressao`. Sem barra, sem “0” e sem fatia vazia |

Se `indicadoresLiberados` for falso, ou se `avisoSupressao` vier preenchido, uma faixa no topo da página repete esse aviso. A frase da API já explica a oclusão de grupos com menos de 5 pessoas. Não reescreva como erro do sistema.

Rodapé do bloco, com `baseLegal`. Deixe explícito na interface: **este painel é a base demográfica. Ele não é o relatório salarial da Lei 14.611/2023.** Não invente campos de salário, cargo ou nome.

#### Bloco de treinamentos (pilar Social)

- `treinamentosObrigatorios`
- `taxaConclusaoGeral`
- Tabela por `departamento`: pessoas ativas, conclusões esperadas, conclusões realizadas, taxa
- `pilarEsg` e `baseLegal`
- Se `aviso` vier preenchido (“nenhum treinamento ativo está marcado como obrigatório”), mostre a faixa e não um gráfico zerado como se fosse fracasso de conclusão

Não liste quem concluiu ou quem falta. A auditoria desta tela é por departamento.

### 3.3 Fila de gestão de denúncias

**Objetivo.** O RH encontra protocolos, lê o caso sem quebrar o anonimato e registra o parecer.

**Quem vê.** RH e Admin veem a fila inteira. Comitê DEI usa sobretudo a fila de escalonamento (seção 3.4).

**Lista.** `GET /api/denuncias`

Colunas:

| Coluna | Origem | Observação |
|---|---|---|
| Protocolo | `protocolo` | Copiável |
| Tipo | `tipoOcorrencia` | Rótulo em português |
| Status | `status` | Mesmos rótulos da seção 2.4 |
| Abertura | `dataCriacao` | |
| Pessoa | `nomeColaborador` | Se `isAnonima` for verdadeiro, a célula mostra “Anônimo” e `nomeColaborador` vem nulo. Não tente buscar o nome em outro endpoint |
| Anônima | `isAnonima` | Selo, além do nome oculto |

Filtros locais: status, tipo, somente anônimas, período. A API entrega a página (`pagina`, `tamanho`, máximo 100). Inclua paginação.

**Detalhe.** `GET /api/denuncias/{protocolo}`

- Descrição do relato.
- Evidências enviadas pela pessoa (`evidencias`), separadas por origem. O download passa pelo painel com a sessão do RH e sai sempre como anexo; não embuta o arquivo numa página nem gere link público. Em caso anônimo, o nome exibido é genérico (`evidencia-1.png`).
- Linha do tempo de `historicos`.
- Se anônima, nenhum nome de denunciante. No histórico da contestação, o autor pode aparecer como “Denunciante”. Mantenha esse texto. Não substitua por um nome.
- Identidade só quando `isAnonima` for falso e `nomeColaborador` vier preenchido.

#### Registro do parecer do RH

Visível somente enquanto a transição para `Tratado` for possível: status `Pendente`, `EmAnalise` ou `Escalonado`.

**Endpoint.** `PUT /api/denuncias/{id}/tratativa`

| Campo na tela | JSON | Regra |
|---|---|---|
| Parecer | `parecerRH` | Obrigatório, até 4.000 caracteres |
| Evidências | `evidenciasAnexadas` | Opcional, até 4.000 caracteres. Texto de referência, não upload |

**Ação.** “Registrar tratativa”. Texto de apoio: “Depois disso, a pessoa poderá confirmar ou contestar no aplicativo.”

Sucesso: o status do detalhe passa a `Tratado` e o formulário fica somente leitura.

Não ofereça este formulário para caso `Encerrado` ou `Contestado`. Se a API recusar a transição, mostre o `erro` do 422.

### 3.4 Painel de escalonamento da diretoria

**Objetivo.** Instância superior enxerga os casos que saíram do fluxo comum: contestados pela pessoa denunciante ou escalonados por prazo.

**Quem vê.** RH, Admin e Comitê DEI. A API ainda não tem o papel “Diretoria”. O alerta de contestação é registrado para “RH superior” e “Diretoria”; na interface, esta tela é o lugar dessa leitura. Quando o papel existir, a mesma tela passa a exigi-lo. Até lá, não crie um login separado.

**Como montar a fila.** `GET /api/denuncias` e manter apenas:

- `status = Contestado`
- `status = Escalonado`

**Cartão do caso.**

- Protocolo, tipo, data de abertura e data de validação (`dataValidacao`), se houver.
- Selo “Contestado pela pessoa denunciante” ou “Escalonado por prazo”.
- Justificativa (`justificativaContestacao`) em destaque quando o status for `Contestado`, com as evidências de origem “Contestacao” logo abaixo.
- Trecho do histórico em que o autor é “Sistema de conformidade”. Há dois momentos: “Contestação registrada… em processamento” (alerta na fila) e “Notificação formal … enviada” (e-mail entregue à diretoria e ao RH superior). Casos vencidos mostram “Prazo regulamentar de N dia(s) excedido…”, gravado junto com o escalonamento automático. Se só existir a primeira linha, mostre “Notificação em envio”, sem tom de erro.
- Mesma proteção de anonimato da fila geral.

**Ação nesta versão.** Leitura e, para RH e Admin, o mesmo parecer da seção 3.3 quando o status ainda for `Escalonado` (a tratativa continua possível). Caso `Contestado` fica em acompanhamento: a tela explica que a contestação já foi encaminhada e não reabre o formulário de validação da pessoa. Não há botão “Confirmar” nem “Contestar” no painel. Esses botões são só do app.

**Vazio.** “Nenhum caso aguarda intervenção superior.”

### 3.5 Cadastro de trilhas (apoio ao painel)

Não é uma das quatro telas pedidas para a primeira leva, mas o dashboard de conclusão depende dela. RH e Admin precisam de um formulário simples.

**Endpoint.** `POST /api/treinamentos`

Campos: título, descrição, categoria, carga horária (0,5 a 400 horas), módulo, conteúdo e o interruptor “Obrigatório”. Para cumprir as Leis 10.639/2003 e 11.645/2008 na métrica, a categoria é `LetramentoEtnicoRacial` e o interruptor fica ligado.

A lista de cursos ativos que o app consome é `GET /api/treinamentos`.

---

## 4. Textos de apoio para a designer

Use estes rótulos de ponta a ponta, no app e no painel, para o mesmo valor da API não ganhar dois nomes.

### Tipos de ocorrência

| API | Interface |
|---|---|
| `AssedioMoral` | Assédio moral |
| `AssedioSexual` | Assédio sexual |
| `DiscriminacaoEtnicoRacial` | Discriminação étnico-racial |
| `DiscriminacaoDeGenero` | Discriminação de gênero |
| `DiscriminacaoPorOrientacaoSexual` | Discriminação por orientação sexual |
| `Capacitismo` | Capacitismo |
| `Retaliacao` | Retaliação |
| `Outro` | Outro |

### O que não desenhar

- Tela de RH com a ficha individual de etnia, gênero, orientação ou deficiência.
- Gráfico demográfico quando `liberado` for falso.
- Nome da pessoa num protocolo marcado como anônimo.
- Upload de evidência pelo RH no parecer. O RH registra só uma referência em texto (`evidenciasAnexadas`); arquivos vêm da pessoa denunciante.
- Pré-visualização de evidência embutida numa página sem login, ou link de download compartilhável. O download sempre exige token.
- Botão de desfazer conclusão de curso.
- Relatório de salário nominal. A Lei 14.611/2023 aparece como base demográfica, não como folha de pagamento.

---

## 5. Mapa rápido de endpoints por tela

| Tela | Método e rota | Quem |
|---|---|---|
| Login (app e painel) | `POST /api/auth/login` | Público |
| Renovação de sessão (sem tela) | `POST /api/auth/refresh` | Público, com access token e refresh token |
| Dashboard e autodeclaração | `GET` e `POST /api/autodeclaracao` | Própria pessoa |
| Abrir relato | `POST /api/denuncias` | Pessoa autenticada |
| Meus relatos / fila do RH | `GET /api/denuncias` | Próprios casos, ou todos se RH/Admin |
| Detalhe do relato | `GET /api/denuncias/{protocolo}` | Autora, ou RH/Admin |
| Parecer do RH | `PUT /api/denuncias/{id}/tratativa` | RH e Admin |
| Confirmar ou contestar | `PUT /api/denuncias/{id}/validacao` | Pessoa que abriu |
| Baixar evidência | `GET /api/denuncias/{protocolo}/evidencias/{evidenciaId}` | Autora, ou RH/Admin |
| Indicadores demográficos | `GET /api/autodeclaracao/relatorios-esg` | RH e Admin |
| Trilhas | `GET /api/treinamentos` | Pessoa autenticada |
| Progresso do curso | `POST /api/treinamentos/{id}/progresso` | Própria pessoa |
| Progresso já gravado | `GET /api/colaboradores/{id}/treinamentos` | Própria pessoa, RH ou Admin |
| Taxa por departamento | `GET /api/treinamentos/relatorios-conclusao` | RH e Admin |
| Cadastrar trilha | `POST /api/treinamentos` | RH e Admin |

Todas as rotas da tabela, exceto login e renovação, exigem `Authorization: Bearer {accessToken}`.
