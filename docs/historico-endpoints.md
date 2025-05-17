# Documentação dos Endpoints - Página de Histórico

## Visão Geral

Esta documentação detalha todos os endpoints necessários para implementar as funcionalidades da página de histórico do sistema Eco-Ipil. A página exibe o histórico de reciclagem dos usuários, estatísticas, gráficos e permite filtragem dos dados.

## Base URL

```
https://api.eco-ipil.com/v1
```

## Autenticação

Todos os endpoints requerem autenticação via token JWT no header:

```
Authorization: Bearer {token}
```

## Endpoints

### 1. Obter Estatísticas Gerais

#### `GET /api/historico/estatisticas`

Retorna as estatísticas gerais mostradas nos cards superiores da página.

**Resposta**
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

### 2. Listar Histórico de Reciclagem

#### `GET /api/historico/reciclagem`

Lista todas as atividades de reciclagem do usuário com suporte a filtros e paginação.

**Parâmetros de Query**
- `material_id`: ID do material (opcional)
- `ecoponto_id`: ID do ecoponto (opcional)
- `data_inicio`: Data inicial (opcional)
- `data_fim`: Data final (opcional)
- `pagina`: Número da página
- `limite`: Itens por página

**Resposta**
```json
{
  "status": true,
  "data": {
    "items": [
      {
        "id": 1,
        "tipo_material": "Plástico",
        "peso": 2.5,
        "ecoponto": "Ecoponto Kilamba",
        "pontos": 50,
        "data": "2024-02-14T14:30:00",
        "material_id": 1,
        "ecoponto_id": 1
      }
    ],
    "total": 100,
    "pagina": 1,
    "total_paginas": 10
  }
}
```

### 3. Obter Dados do Gráfico

#### `GET /api/historico/grafico`

Retorna dados para o gráfico de linha mostrando a evolução da reciclagem.

**Parâmetros de Query**
- `periodo`: "mensal" | "semanal" | "anual" (default: mensal)
- `ano`: Ano (opcional)
- `mes`: Mês (opcional)

**Resposta**
```json
{
  "status": true,
  "data": {
    "labels": ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun"],
    "dados": [15, 22, 18, 25, 30, 27]
  }
}
```

### 4. Listar Materiais para Filtro

#### `GET /api/materiais`

Retorna a lista de materiais disponíveis para o filtro.

**Resposta**
```json
{
  "status": true,
  "data": [
    {
      "id": 1,
      "nome": "Plástico",
      "classe": "Reciclável"
    }
  ]
}
```

### 5. Listar Ecopontos para Filtro

#### `GET /api/ecopontos/lista`

Retorna a lista de ecopontos disponíveis para o filtro.

**Resposta**
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

### 6. Obter Detalhes de uma Reciclagem

#### `GET /api/historico/reciclagem/{id}`

Retorna detalhes completos de uma atividade específica de reciclagem.

**Parâmetros de Path**
- `id`: ID da reciclagem

**Resposta**
```json
{
  "status": true,
  "data": {
    "id": 1,
    "data": "2024-02-14T14:30:00",
    "material": {
      "id": 1,
      "nome": "Plástico",
      "valor": 20
    },
    "peso": 2.5,
    "ecoponto": {
      "id": 1,
      "nome": "Ecoponto Kilamba",
      "localizacao": "Kilamba"
    },
    "pontos_ganhos": 50,
    "agente": {
      "id": 1,
      "nome": "João Silva"
    }
  }
}
```

### 7. Obter Impacto Ambiental

#### `GET /api/historico/impacto-ambiental`

Retorna métricas de impacto ambiental baseadas nas atividades de reciclagem.

**Resposta**
```json
{
  "status": true,
  "data": {
    "arvores_salvas": 3,
    "co2_evitado": 127.5,
    "agua_economizada": 1500,
    "energia_economizada": 250
  }
}
```

## Códigos de Status HTTP

- `200` - OK: Requisição bem-sucedida
- `400` - Bad Request: Parâmetros inválidos
- `401` - Unauthorized: Token inválido ou expirado
- `403` - Forbidden: Sem permissão para acessar o recurso
- `404` - Not Found: Recurso não encontrado
- `500` - Internal Server Error: Erro interno do servidor

## Formato de Erro

```json
{
  "status": false,
  "error": {
    "code": "INVALID_PARAMETER",
    "message": "Parâmetro inválido",
    "details": ["O campo data_inicio deve ser uma data válida"]
  }
}
```

## Query SQL de Exemplo

```sql
SELECT 
    r.id,
    r.created_at,
    r.peso,
    m.nome as material_nome,
    m.valor as pontos_por_kg,
    e.nome as ecoponto_nome,
    e.localizacao as ecoponto_localizacao,
    a.nome as agente_nome
FROM reciclagem r
LEFT JOIN materiais m ON r.material_id = m.id
LEFT JOIN ecopontos e ON r.ecoponto_id = e.id
LEFT JOIN agentes a ON r.agente_id = a.id
WHERE r.usuario_id = :usuario_id
    AND (r.material_id = :material_id OR :material_id IS NULL)
    AND (r.ecoponto_id = :ecoponto_id OR :ecoponto_id IS NULL)
    AND (r.created_at BETWEEN :data_inicio AND :data_fim)
ORDER BY r.created_at DESC
LIMIT :limite OFFSET :offset;
```

## Considerações de Implementação

1. **Cache**
   - Implementar cache para dados estáticos (lista de materiais e ecopontos)
   - Definir tempo de expiração apropriado para cada tipo de dado

2. **Paginação**
   - Todos os endpoints que retornam listas devem implementar paginação
   - Tamanho padrão da página: 10 itens
   - Máximo de itens por página: 100

3. **Performance**
   - Implementar índices apropriados no banco de dados
   - Otimizar queries para grandes volumes de dados
   - Considerar implementação de cache em camadas

4. **Segurança**
   - Validar todos os inputs
   - Implementar rate limiting
   - Sanitizar dados de saída
   - Implementar logs de auditoria para operações sensíveis

5. **Real-time**
   - Considerar implementação de WebSocket para atualizações em tempo real
   - Endpoint de fallback para clientes sem suporte a WebSocket 