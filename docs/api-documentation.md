# Documentação da API do Eco-Ipil

## Visão Geral

Esta documentação detalha os endpoints necessários para a API do sistema Eco-Ipil, baseada na estrutura do banco de dados fornecido e nas funcionalidades identificadas na aplicação React.

## Base URL

```
https://api.eco-ipil.com/v1
```

## Autenticação

A API utiliza autenticação JWT (JSON Web Token). Todos os endpoints privados requerem um token válido no cabeçalho de autorização:

```
Authorization: Bearer {token}
```

---

## 1. Autenticação e Gestão de Usuários

### 1.1. Autenticação

#### `POST /auth/login`
- **Descrição**: Autenticar usuário, agente ou administrador
- **Parâmetros do corpo**:
  - `email`: Email do usuário
  - `senha`: Senha do usuário
  - `tipo`: Tipo de usuário (usuario, agente, admin)
  - `manterConectado`: Boolean para manter sessão ativa

#### `POST /auth/register`
- **Descrição**: Registrar novo usuário
- **Parâmetros do corpo**:
  - `nome`: Nome completo
  - `email`: Email
  - `senha`: Senha
  - `telefone`: Número de telefone
  - `genero`: Gênero
  - `dataNascimento`: Data de nascimento

#### `POST /auth/forgot-password`
- **Descrição**: Solicitar redefinição de senha
- **Parâmetros do corpo**:
  - `email`: Email do usuário

#### `POST /auth/reset-password`
- **Descrição**: Redefinir senha
- **Parâmetros do corpo**:
  - `token`: Token de redefinição de senha
  - `novaSenha`: Nova senha

#### `POST /auth/logout`
- **Descrição**: Encerrar sessão do usuário

### 1.2. Gestão de Usuários

#### `GET /usuarios/perfil`
- **Descrição**: Obter dados do perfil do usuário logado
- **Autenticação**: Requerida

#### `PUT /usuarios/perfil`
- **Descrição**: Atualizar dados do perfil
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `nome`: Nome completo
  - `telefone`: Número de telefone
  - `genero`: Gênero
  - `localizacao`: Localização do usuário

#### `PUT /usuarios/senha`
- **Descrição**: Atualizar senha
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `senhaAtual`: Senha atual
  - `novaSenha`: Nova senha

#### `POST /usuarios/foto`
- **Descrição**: Fazer upload de foto de perfil
- **Autenticação**: Requerida
- **Parâmetros**: Arquivo multimídia

---

## 2. Carteira Digital e Pontos

### 2.1. Carteira

#### `GET /carteira`
- **Descrição**: Obter dados da carteira digital do usuário
- **Autenticação**: Requerida

#### `GET /carteira/historico`
- **Descrição**: Obter histórico de transações da carteira
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `pagina`: Número da página
  - `limite`: Itens por página
  - `tipo`: Tipo de transação (opcional)

#### `POST /carteira/transferir`
- **Descrição**: Transferir saldo ou pontos para outro usuário
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `destinatarioId`: ID do usuário destinatário
  - `valor`: Valor a transferir
  - `tipo`: "pontos" ou "saldo"

---

## 3. Reciclagem

### 3.1. Ecopontos

#### `GET /ecopontos`
- **Descrição**: Listar todos os ecopontos
- **Parâmetros de consulta**:
  - `latitude`: Latitude para busca por proximidade
  - `longitude`: Longitude para busca por proximidade
  - `raio`: Raio de busca em km
  - `material`: Filtro por tipo de material
  - `status`: Filtro por status do ecoponto

#### `GET /ecopontos/{id}`
- **Descrição**: Obter detalhes de um ecoponto específico
- **Parâmetros de caminho**:
  - `id`: ID do ecoponto

#### `GET /ecopontos/lista`
- **Descrição**: Retorna uma lista simplificada de ecopontos para uso em filtros
- **Resposta**:
```json
{
  "status": true,
  "data": [
    {
      "id": 1,
      "nome": "Ecoponto Kilamba",
      "status": "Ativo"
    }
  ]
}
```

### 3.2. Histórico de Reciclagem

#### `GET /historico/estatisticas`
- **Descrição**: Retorna as estatísticas gerais do usuário
- **Autenticação**: Requerida
- **Resposta**:
```json
{
  "status": true,
  "data": {
    "total_reciclado": 127.5,
    "pontos_acumulados": 2550,
    "visitas_ecoponto": 15,
    "arvores_salvas": 3
  }
}
```

#### `GET /historico/grafico`
- **Descrição**: Retorna dados para o gráfico de evolução da reciclagem
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `periodo`: "mensal" | "semanal" | "anual" (default: mensal)
  - `ano`: Ano (opcional)
  - `mes`: Mês (opcional)
- **Resposta**:
```json
{
  "status": true,
  "data": {
    "labels": ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun"],
    "dados": [15, 22, 18, 25, 30, 27]
  }
}
```

### 3.3. Materiais

#### `GET /materiais`
- **Descrição**: Listar todos os materiais recicláveis
- **Parâmetros de consulta**:
  - `classe`: Filtro por classe de material

#### `GET /materiais/{id}`
- **Descrição**: Obter detalhes de um material específico
- **Parâmetros de caminho**:
  - `id`: ID do material

### 3.4. Processo de Reciclagem

#### `POST /reciclagem/escanear`
- **Descrição**: Processar código QR escaneado
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `codigoQR`: Código QR lido

#### `POST /reciclagem/registrar`
- **Descrição**: Registrar nova reciclagem
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `materialId`: ID do material reciclado
  - `peso`: Peso do material
  - `ecopontoId`: ID do ecoponto
  
#### `GET /reciclagem/historico`
- **Descrição**: Obter histórico de reciclagem do usuário
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `pagina`: Número da página
  - `limite`: Itens por página
  - `dataInicio`: Filtro por data inicial
  - `dataFim`: Filtro por data final
  - `materialId`: Filtro por material

#### `GET /reciclagem/estatisticas`
- **Descrição**: Obter estatísticas de reciclagem do usuário
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `periodo`: Período (semana, mes, ano, total)

---

## 4. Recompensas e Campanhas

### 4.1. Recompensas

#### `GET /recompensas`
- **Descrição**: Listar recompensas disponíveis
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `tipo`: Tipo de recompensa
  - `precoMin`: Preço mínimo em pontos
  - `precoMax`: Preço máximo em pontos

#### `GET /recompensas/{id}`
- **Descrição**: Obter detalhes de uma recompensa
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID da recompensa

#### `POST /recompensas/{id}/resgatar`
- **Descrição**: Resgatar uma recompensa
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID da recompensa

#### `GET /recompensas/usuario`
- **Descrição**: Listar recompensas do usuário
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `status`: Status da recompensa (resgatada, utilizada)



### 4.2. Campanhas

#### `GET /campanhas`
- **Descrição**: Listar campanhas ativas
- **Autenticação**: Requerida

#### `GET /campanhas/{id}`
- **Descrição**: Obter detalhes de uma campanha
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID da campanha

#### `POST /campanhas/{id}/participar`
- **Descrição**: Participar de uma campanha
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID da campanha

### 4.3. Conquistas

#### `GET /conquistas`
- **Descrição**: Listar todas as conquistas
- **Autenticação**: Requerida

#### `GET /conquistas/usuario`
- **Descrição**: Listar conquistas do usuário
- **Autenticação**: Requerida

---

## 5. Investimentos

#### `GET /investimentos`
- **Descrição**: Listar opções de investimento
- **Autenticação**: Requerida

#### `GET /investimentos/{id}`
- **Descrição**: Obter detalhes de um investimento
- **Autenticação**: Requerida (pedir token como parametro)
- **Parâmetros de caminho**:
  - `id`: ID do investimento

#### `POST /investimentos/{id}/investir`
- **Descrição**: Realizar investimento
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID do investimento
- **Parâmetros do corpo**:
  - `pontos`: Quantidade de pontos a investir

#### `GET /investimentos/usuario`
- **Descrição**: Listar investimentos do usuário
- **Autenticação**: Requerida (pedir token como parametro)


### 5.1. ATÉ AQUI TUDO FEITO
---

## 6. Dashboard

#### `GET /dashboard/resumo`
- **Descrição**: Obter resumo para o dashboard
- **Autenticação**: Requerida

#### `GET /dashboard/estatisticas`
- **Descrição**: Obter estatísticas detalhadas para gráficos
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `tipo`: Tipo de estatística (reciclagem, pontos, etc)
  - `periodo`: Período (semana, mes, ano)

#### `GET /dashboard/impacto-ambiental`
- **Descrição**: Obter métricas de impacto ambiental
- **Autenticação**: Requerida

---

## 7. Notificações e Tickets

### 7.1. Notificações

#### `GET /notificacoes`
- **Descrição**: Listar notificações do usuário
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `lida`: Filtro por status de leitura
  - `pagina`: Número da página
  - `limite`: Itens por página

#### `PUT /notificacoes/{id}/ler`
- **Descrição**: Marcar notificação como lida
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID da notificação

#### `PUT /notificacoes/ler-todas`
- **Descrição**: Marcar todas notificações como lidas
- **Autenticação**: Requerida

### 7.2. Tickets

#### `GET /tickets`
- **Descrição**: Listar tickets do usuário
- **Autenticação**: Requerida
- **Parâmetros de consulta**:
  - `status`: Filtro por status
  - `pagina`: Número da página
  - `limite`: Itens por página

#### `GET /tickets/{id}`
- **Descrição**: Obter detalhes de um ticket
- **Autenticação**: Requerida
- **Parâmetros de caminho**:
  - `id`: ID do ticket

#### `POST /tickets/gerar`
- **Descrição**: Gerar novo ticket (ex: para saque)
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `tipo`: Tipo de operação
  - `valor`: Valor do ticket
  - `descricao`: Descrição do ticket

---

## 8. Configurações e Preferências

#### `GET /configuracoes`
- **Descrição**: Obter configurações do usuário
- **Autenticação**: Requerida

#### `PUT /configuracoes`
- **Descrição**: Atualizar configurações do usuário
- **Autenticação**: Requerida
- **Parâmetros do corpo**:
  - `notificacoes`: Preferências de notificação
  - `privacidade`: Configurações de privacidade
  - `tema`: Preferência de tema (claro/escuro)

---

## Códigos de Status HTTP

- `200` - OK: Requisição bem-sucedida
- `201` - Created: Recurso criado com sucesso
- `400` - Bad Request: Parâmetros inválidos
- `401` - Unauthorized: Autenticação necessária
- `403` - Forbidden: Sem permissão para acessar o recurso
- `404` - Not Found: Recurso não encontrado
- `422` - Unprocessable Entity: Validação falhou
- `500` - Internal Server Error: Erro interno do servidor

## Formato de Resposta Padrão

Todas as respostas seguem o formato:

```json
{
  "status": true/false,
  "data": { ... },
  "message": "Mensagem descritiva",
  "errors": [ ... ] // apenas quando há erros
}
```

## Considerações de Segurança

1. Todas as requisições devem ser feitas via HTTPS
2. Tokens JWT expiram após 1 hora (configurável)
3. Implementar limitação de taxa (rate limiting) para evitar abuso
4. Validar todos os inputs no servidor

## Paginação

Endpoints que retornam listas suportam paginação via parâmetros `pagina` e `limite`. A resposta inclui metadados:

```json
{
  "status": true,
  "data": [ ... ],
  "meta": {
    "total": 100,
    "pagina": 1,
    "limite": 10,
    "paginas": 10
  }
}
``` 